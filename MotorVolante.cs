using System;
using System.Threading;

namespace Volante;

/// <summary>
/// Lê o mouse do volante (Interception) e escreve o eixo e os botões no vJoy.
/// O laço de entrada roda numa thread própria de alta prioridade.
/// Quem usa o motor (console ou interface) só lê o estado e escuta os eventos.
/// </summary>
sealed class MotorVolante : IDisposable
{
    // ---------------- Ajustes internos ----------------
    const int TamanhoLote = 64;
    const int EsperaMaximaMs = 20;
    const int IntervaloTeclasMs = 10;
    const int IntervaloVerificacaoMouseMs = 250;
    const int FalhasParaDesconectar = 2;

    const ushort FlagRoda = 0x400;

    readonly struct MapaBotao
    {
        public readonly ushort Down;
        public readonly ushort Up;
        public readonly byte Botao;

        public MapaBotao(ushort down, ushort up, byte botao)
        {
            Down = down;
            Up = up;
            Botao = botao;
        }
    }

    // Bits de estado do Interception -> botão do vJoy.
    static readonly MapaBotao[] MapaBotoes =
    {
        new MapaBotao(0x01, 0x02, 1),   // esquerdo
        new MapaBotao(0x04, 0x08, 2),   // direito
        new MapaBotao(0x10, 0x20, 3),   // meio
    };

    // ---------------- Estado ----------------
    readonly Configuracao _cfg;
    readonly object _medicaoLock = new object();
    readonly Stroke[] _lote = new Stroke[TamanhoLote];

    IntPtr _contexto = IntPtr.Zero;
    PredicadoInterception _filtro;
    int _mouse;
    bool _vJoyAdquirido;

    int _minimo;
    int _maximo;
    int _centro;
    int _ultimoValorEixo;

    Thread _thread;
    volatile bool _executando;
    volatile bool _pedidoRecentralizar;
    volatile bool _configAlterada;
    volatile bool _diagnosticoAlterado;
    volatile bool _medindo;
    long _contagemBruta;

    // Escritos só pela thread do motor; lidos de qualquer thread via Volatile.
    int _contagens;
    int _botoes;
    int _entradasMouse;
    bool _botaoMeioPressionado;

    long _proximaChecagem;
    long _proximaVerificacaoMouse;
    int _falhasVerificacaoMouse;
    bool _teclaRecentralizarAnterior;
    bool _teclaContagemAnterior;
    long _soltarRodaCima;
    long _soltarRodaBaixo;

    DiagnosticoEntrada _diag;

    public MotorVolante(Configuracao configuracao)
    {
        _cfg = configuracao;
    }

    // ---------------- API pública ----------------

    /// <summary>Mensagens de status. Pode ser chamado de outra thread, não bloqueie nele.</summary>
    public event Action<string> Mensagem;

    /// <summary>Mensagens do diagnóstico, separadas dos avisos gerais do motor.</summary>
    public event Action<string> DiagnosticoMensagem;

    /// <summary>Disparado quando o laço termina (por Parar ou por erro).</summary>
    public event Action Parou;

    public bool Rodando => _executando;

    /// <summary>Posição acumulada do volante, em contagens do sensor.</summary>
    public int Contagens => Volatile.Read(ref _contagens);

    public double Graus => Contagens * _cfg.GrausPorContagem;

    /// <summary>De -1 (batente esquerdo) a +1 (batente direito).</summary>
    public double Proporcao => (double)Contagens / LimiteContagens;

    /// <summary>Máscara de botões do vJoy pressionados (bit 0 = botão 1).</summary>
    public int Botoes => Volatile.Read(ref _botoes);

    /// <summary>Entradas do mouse: esquerdo, direito, clique do scroll e roda para cima/baixo.</summary>
    public int EntradasMouse => Volatile.Read(ref _entradasMouse);

    /// <summary>Pede para recentralizar; é atendido em até ~20 ms.</summary>
    public void SolicitarRecentralizacao()
    {
        _pedidoRecentralizar = true;
    }

    /// <summary>Avisa que a configuração mudou (ex.: curso). O eixo é reaplicado em até ~20 ms.</summary>
    public void ConfiguracaoAlterada()
    {
        _configAlterada = true;
    }

    /// <summary>Avisa que o modo de diagnóstico foi ligado ou desligado.</summary>
    public void DiagnosticoAlterado()
    {
        _diagnosticoAlterado = true;
    }

    /// <summary>Começa a somar o movimento bruto do sensor (para calibrar graus por contagem).</summary>
    public void IniciarMedicao()
    {
        lock (_medicaoLock)
        {
            _contagemBruta = 0;
            _medindo = true;
        }
    }

    /// <summary>Encerra a medição e devolve a soma bruta (com sinal).</summary>
    public long PararMedicao()
    {
        lock (_medicaoLock)
        {
            _medindo = false;
            return _contagemBruta;
        }
    }

    public void Iniciar()
    {
        if (_executando)
            return;

        if (_thread != null && _thread.IsAlive)
            throw new InvalidOperationException("O motor ainda está encerrando. Tente novamente em instantes.");

        try
        {
            if (!double.IsFinite(_cfg.GrausPorContagem) ||
                _cfg.GrausPorContagem < 0.001 ||
                _cfg.GrausPorContagem > 1.0 ||
                !double.IsFinite(_cfg.LimiteGraus) ||
                _cfg.LimiteGraus < 45.0 ||
                _cfg.LimiteGraus > 900.0)
                throw new InvalidOperationException("A calibração e o curso do volante estão fora dos limites permitidos.");

            if (!VJoy.Habilitado())
                throw new InvalidOperationException("vJoy não está habilitado.");

            _contexto = Interception.CriarContexto();

            if (_contexto == IntPtr.Zero)
                throw new InvalidOperationException("Não foi possível iniciar o Interception.");

            _mouse = Interception.EncontrarMouse(_contexto, _cfg.HardwareIdMouse);

            if (_mouse == 0)
                throw new InvalidOperationException("Mouse do volante não encontrado.");

            _proximaVerificacaoMouse = 0;
            _falhasVerificacaoMouse = 0;

            if (!VJoy.Adquirir(_cfg.VJoyId))
                throw new InvalidOperationException($"Não foi possível adquirir o vJoy {_cfg.VJoyId}.");

            _vJoyAdquirido = true;
            if (!VJoy.Zerar(_cfg.VJoyId))
                throw new InvalidOperationException("Não foi possível zerar os controles do vJoy.");

            if (!VJoy.LimiteMinimo(_cfg.VJoyId, VJoy.EixoX, out _minimo) ||
                !VJoy.LimiteMaximo(_cfg.VJoyId, VJoy.EixoX, out _maximo))
                throw new InvalidOperationException(
                    "Não foi possível obter os limites do eixo X. Confira o vJoyConf.");

            if (_maximo <= _minimo)
                throw new InvalidOperationException("O vJoy informou limites inválidos para o eixo X.");

            _centro = (int)(((long)_minimo + _maximo) / 2);

            Volatile.Write(ref _contagens, 0);
            Volatile.Write(ref _botoes, 0);
            Volatile.Write(ref _entradasMouse, 0);
            _botaoMeioPressionado = false;
            _ultimoValorEixo = int.MinValue;
            DefinirEixo(_centro);

            // Pega movimento, botões e roda. Como nada é reenviado (interception_send),
            // o sistema não vê esse mouse.
            _filtro = SelecionarMouse;
            Interception.DefinirFiltro(_contexto, _filtro, Interception.FiltroMouseTudo);

            _diag = _cfg.Diagnostico ? new DiagnosticoEntrada(PublicarDiagnostico) : null;
            _diagnosticoAlterado = false;
        }
        catch
        {
            Liberar();
            throw;
        }

        try
        {
            _thread = new Thread(Laco)
            {
                Name = "Volante-Entrada",
                IsBackground = true,
                Priority = ThreadPriority.Highest,
            };

            _executando = true;
            _thread.Start();
        }
        catch
        {
            _executando = false;
            _thread = null;
            Liberar();
            throw;
        }
    }

    public void Parar()
    {
        _executando = false;

        Thread t = _thread;

        if (t == null)
        {
            Liberar();
            return;
        }

        if (t == Thread.CurrentThread)
            return;

        if (!t.Join(2000))
        {
            Publicar("O motor ainda está encerrando; os recursos serão liberados quando a thread terminar.");
        }
    }

    public void Dispose()
    {
        Parar();
    }

    // ---------------- Laço principal ----------------

    void Laco()
    {
        try
        {
            while (_executando)
            {
                long agora = Environment.TickCount64;

                if (agora >= _proximaVerificacaoMouse)
                {
                    _proximaVerificacaoMouse = agora + IntervaloVerificacaoMouseMs;

                    if (Interception.MouseConectado(_contexto, _mouse, _cfg.HardwareIdMouse))
                    {
                        _falhasVerificacaoMouse = 0;
                    }
                    else if (++_falhasVerificacaoMouse >= FalhasParaDesconectar)
                    {
                        Publicar("Mouse do volante desconectado. O motor foi parado.");
                        break;
                    }
                }

                if (agora >= _proximaChecagem)
                {
                    _proximaChecagem = agora + IntervaloTeclasMs;

                    ChecarTeclas();
                    SoltarPulsosRoda(agora);
                    _diag?.Relatorio(agora);
                }

                if (_pedidoRecentralizar)
                {
                    _pedidoRecentralizar = false;
                    AplicarRecentralizacao();
                }

                if (_configAlterada)
                {
                    _configAlterada = false;

                    int novoLimite = LimiteContagens;
                    Volatile.Write(ref _contagens, Math.Clamp(_contagens, -novoLimite, novoLimite));
                    AtualizarEixo(novoLimite);

                    bool meioDeveEstarPressionado =
                        _botaoMeioPressionado && !_cfg.BotaoMeioRecentraliza;
                    bool meioEstaPressionado = (Volatile.Read(ref _botoes) & (1 << 2)) != 0;

                    if (meioDeveEstarPressionado != meioEstaPressionado)
                        DefinirBotao(3, meioDeveEstarPressionado);
                }

                if (_diagnosticoAlterado)
                {
                    _diagnosticoAlterado = false;

                    if (_cfg.Diagnostico)
                    {
                        _diag = new DiagnosticoEntrada(PublicarDiagnostico);
                        _teclaContagemAnterior = Teclado.Pressionada(_cfg.TeclaContagem);
                    }
                    else
                    {
                        _diag?.Desativar();
                        _diag = null;
                        _teclaContagemAnterior = false;
                    }
                }

                int dispositivo = Interception.Esperar(_contexto, EsperaMaximaMs);

                if (dispositivo != _mouse)
                    continue;

                int recebidos = Interception.Receber(_contexto, dispositivo, _lote, (uint)_lote.Length);

                if (recebidos <= 0)
                    continue;

                ProcessarLote(recebidos);
            }
        }
        catch (Exception erro)
        {
            Publicar("ERRO: " + erro.Message);
        }
        finally
        {
            _executando = false;
            Liberar();
            Parou?.Invoke();
        }
    }

    void ProcessarLote(int quantidade)
    {
        _diag?.RegistrarLote(quantidade);

        int limite = LimiteContagens;
        bool moveu = false;

        for (int i = 0; i < quantidade; i++)
        {
            ushort estado = _lote[i].mouse.state;

            // ----- Movimento -----
            int dy = _lote[i].mouse.y;

            if (dy != 0)
            {
                if (_medindo)
                {
                    lock (_medicaoLock)
                    {
                        if (_medindo)
                            _contagemBruta += dy;
                    }
                }

                _diag?.RegistrarMovimento(dy);

                long delta = _cfg.InverterSentido ? -(long)dy : dy;
                long novo = _contagens + delta;
                Volatile.Write(
                    ref _contagens,
                    (int)Math.Clamp(novo, -(long)limite, (long)limite));
                moveu = true;
            }

            ProcessarBotoes(estado);
            ProcessarRoda(estado, _lote[i].mouse.rolling);
        }

        // Uma única escrita no vJoy por lote (e só se o valor mudou).
        if (moveu)
            AtualizarEixo(limite);
    }

    void ProcessarBotoes(ushort estado)
    {
        for (int k = 0; k < MapaBotoes.Length; k++)
        {
            MapaBotao m = MapaBotoes[k];

            if (m.Botao == 3)
            {
                if ((estado & m.Down) != 0)
                    _botaoMeioPressionado = true;

                if ((estado & m.Up) != 0)
                    _botaoMeioPressionado = false;
            }

            if (m.Botao <= 3)
            {
                if ((estado & m.Down) != 0)
                    DefinirEntradaMouse(m.Botao - 1, true);

                if ((estado & m.Up) != 0)
                    DefinirEntradaMouse(m.Botao - 1, false);
            }

            bool recentraliza = m.Botao == 3 && _cfg.BotaoMeioRecentraliza;

            if (recentraliza)
            {
                if ((estado & m.Down) != 0)
                    AplicarRecentralizacao();

                if ((estado & m.Up) != 0 &&
                    (Volatile.Read(ref _botoes) & (1 << (m.Botao - 1))) != 0)
                    DefinirBotao(m.Botao, false);

                continue;
            }

            if ((estado & m.Down) != 0)
                DefinirBotao(m.Botao, true);

            if ((estado & m.Up) != 0)
                DefinirBotao(m.Botao, false);
        }
    }

    void ProcessarRoda(ushort estado, short rolling)
    {
        if ((estado & FlagRoda) == 0)
            return;

        long ate = Environment.TickCount64 + _cfg.PulsoRodaMs;

        if (rolling > 0)
        {
            DefinirEntradaMouse(3, true);
            DefinirBotao(_cfg.BotaoRodaCima, true);
            _soltarRodaCima = ate;
        }
        else if (rolling < 0)
        {
            DefinirEntradaMouse(4, true);
            DefinirBotao(_cfg.BotaoRodaBaixo, true);
            _soltarRodaBaixo = ate;
        }
    }

    void SoltarPulsosRoda(long agora)
    {
        if (_soltarRodaCima != 0 && agora >= _soltarRodaCima)
        {
            DefinirEntradaMouse(3, false);
            DefinirBotao(_cfg.BotaoRodaCima, false);
            _soltarRodaCima = 0;
        }

        if (_soltarRodaBaixo != 0 && agora >= _soltarRodaBaixo)
        {
            DefinirEntradaMouse(4, false);
            DefinirBotao(_cfg.BotaoRodaBaixo, false);
            _soltarRodaBaixo = 0;
        }
    }

    void ChecarTeclas()
    {
        // Só reage na borda de subida (quando a tecla acabou de ser apertada).
        bool recentralizar = Teclado.Pressionada(_cfg.TeclaRecentralizar);

        if (recentralizar && !_teclaRecentralizarAnterior)
            AplicarRecentralizacao();

        _teclaRecentralizarAnterior = recentralizar;

        if (_diag != null)
        {
            bool contagem = Teclado.Pressionada(_cfg.TeclaContagem);

            if (contagem && !_teclaContagemAnterior)
                _diag.AlternarContagem();

            _teclaContagemAnterior = contagem;
        }
    }

    // ---------------- vJoy ----------------

    void AplicarRecentralizacao()
    {
        Volatile.Write(ref _contagens, 0);
        DefinirEixo(_centro);
        Publicar("Recentralizado.");
    }

    void AtualizarEixo(int limite)
    {
        double proporcao = Math.Clamp((double)_contagens / limite, -1.0, 1.0);
        double coordenada = _centro + proporcao * ((double)_maximo - _minimo) / 2.0;
        int valor = (int)Math.Round(Math.Clamp(coordenada, _minimo, _maximo));

        DefinirEixo(valor);
    }

    // Só escreve no vJoy quando o valor realmente mudou.
    void DefinirEixo(int valor)
    {
        if (valor == _ultimoValorEixo)
            return;

        if (!VJoy.DefinirEixo(valor, _cfg.VJoyId, VJoy.EixoX))
            throw new InvalidOperationException("O vJoy recusou a atualização do eixo X.");

        _ultimoValorEixo = valor;
    }

    void DefinirBotao(byte botao, bool pressionado)
    {
        if (!VJoy.DefinirBotao(pressionado, _cfg.VJoyId, botao))
            throw new InvalidOperationException($"O vJoy recusou a atualização do botão {botao}.");

        int bit = 1 << (botao - 1);
        int atual = _botoes;

        Volatile.Write(ref _botoes, pressionado ? (atual | bit) : (atual & ~bit));
    }

    void DefinirEntradaMouse(int entrada, bool pressionada)
    {
        int bit = 1 << entrada;
        int atual = _entradasMouse;

        Volatile.Write(ref _entradasMouse, pressionada ? (atual | bit) : (atual & ~bit));
    }

    // ---------------- Auxiliares ----------------

    int LimiteContagens => Math.Max(1, (int)Math.Round(_cfg.LimiteGraus / _cfg.GrausPorContagem));

    int SelecionarMouse(int dispositivo)
    {
        return dispositivo == _mouse ? 1 : 0;
    }

    void Publicar(string texto)
    {
        Mensagem?.Invoke(texto);
    }

    void PublicarDiagnostico(string texto)
    {
        DiagnosticoMensagem?.Invoke(texto);
    }

    // Seguro chamar mais de uma vez.
    void Liberar()
    {
        if (_vJoyAdquirido)
        {
            if (!VJoy.Zerar(_cfg.VJoyId))
                Publicar("ERRO: não foi possível zerar os controles do vJoy ao encerrar.");

            VJoy.Liberar(_cfg.VJoyId);
            _vJoyAdquirido = false;
        }

        if (_contexto != IntPtr.Zero)
        {
            Interception.DestruirContexto(_contexto);
            _contexto = IntPtr.Zero;
        }
    }
}
