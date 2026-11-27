using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Text.Json;
using System.Windows.Forms;

namespace Volante;

/// <summary>Janela simples para acompanhar o volante e ajustar sensibilidade e calibração.</summary>
sealed class JanelaPrincipal : Form
{
    readonly Configuracao _cfg;
    readonly MotorVolante _motor;
    JanelaDiagnostico _janelaDiagnostico;

    readonly Timer _timerTela = new Timer { Interval = 33 };
    readonly Timer _timerSalvar = new Timer { Interval = 500 };
    readonly Timer _timerAvisoRecentralizacao = new Timer { Interval = 2000 };

    Label _lblStatus;
    Label _lblMensagem;
    Label _lblAvisoRecentralizacao;
    Label _lblCurso;
    Label _lblVoltasCurso;
    Label _lblVoltas;
    Label _lblMedicao;
    Button _btnReconectar;
    Button _btnMedir;
    TrackBar _trackCurso;
    TrackBar _trackVoltas;
    CheckBox _chkInverter;
    CheckBox _chkMeio;
    NumericUpDown _numGraus;
    PainelPosicao _painelPosicao;
    PainelEntradasMouse _painelEntradasMouse;

    bool _medindo;
    bool _ultimoRodando;
    string _erroSalvar;

    [DllImport("dwmapi.dll")]
    static extern int DwmSetWindowAttribute(IntPtr janela, int atributo, ref int valor, int tamanho);

    public JanelaPrincipal(Configuracao cfg, MotorVolante motor)
    {
        _cfg = cfg;
        _motor = motor;

        ConstruirTela();
        AplicarConfiguracaoNaTela();
        LigarEventos();
        _janelaDiagnostico = new JanelaDiagnostico(_cfg, _motor, ProgramarSalvamento);
    }

    void MostrarMensagemMotor(string texto)
    {
        if (texto == "Recentralizado.")
        {
            _lblAvisoRecentralizacao.Text = texto;
            _timerAvisoRecentralizacao.Stop();
            _timerAvisoRecentralizacao.Start();
            return;
        }

        _lblMensagem.ForeColor = texto.StartsWith("ERRO:", StringComparison.Ordinal)
            ? Tema.Erro
            : Tema.Suave;
        _lblMensagem.Text = texto;
    }

    // ---------------- Montagem da tela ----------------

    void ConstruirTela()
    {
        SuspendLayout();

        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;

        Font = new Font("Segoe UI", 9F);
        Text = "Volante";
        BackColor = Tema.Fundo;
        ForeColor = Tema.Texto;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        DoubleBuffered = true;

        var negrito = new Font("Segoe UI", 9F, FontStyle.Bold);
        var pequeno = new Font("Segoe UI", 8F);
        var secao = new Font("Segoe UI", 8F, FontStyle.Bold);
        var titulo = new Font("Segoe UI", 17F, FontStyle.Bold);

        const int margem = 20;
        const int largura = 400;
        int y = 14;

        // Cabeçalho
        Controls.Add(NovoRotulo("Volante", margem, y, 240, 34, titulo, Tema.Texto, Tema.Fundo));
        y += 36;

        _lblStatus = NovoRotulo("", margem, y, 260, 22, Font, Tema.Suave, Tema.Fundo);
        Controls.Add(_lblStatus);

        _btnReconectar = NovoBotao("Reconectar", margem + largura - 110, y - 3, 110, 28, true);
        _btnReconectar.Visible = false;
        Controls.Add(_btnReconectar);
        y += 34;

        // Posição do volante
        _painelPosicao = new PainelPosicao
        {
            Location = new Point(margem, y),
            Size = new Size(largura, 120),
        };
        Controls.Add(_painelPosicao);

        y += 120 + 4;
        _lblAvisoRecentralizacao = NovoRotulo(
            "", margem, y, largura, 20, Font, Tema.Ok, Tema.Fundo,
            ContentAlignment.MiddleLeft);
        Controls.Add(_lblAvisoRecentralizacao);
        y += 20 + 4;

        var btnRecentralizar = NovoBotao("Recentralizar (F9)", margem, y, largura, 38, true);
        btnRecentralizar.Click += (s, e) => _motor.SolicitarRecentralizacao();
        Controls.Add(btnRecentralizar);
        y += 38 + 10;

        // Teste de entradas do mouse
        var textoTeste = new Panel
        {
            Location = new Point(margem, y),
            Size = new Size(largura, 36),
            BackColor = Tema.Fundo,
        };
        textoTeste.Controls.Add(NovoRotulo(
            "TESTE DOS COMANDOS DO MOUSE", 0, 0, largura, 18, secao, Tema.Suave, Tema.Fundo));
        textoTeste.Controls.Add(NovoRotulo(
            "Clique nos botões ou gire a roda para ver os indicadores acenderem.",
            0, 17, largura, 18, pequeno, Tema.Suave, Tema.Fundo));
        Controls.Add(textoTeste);
        y += 36 + 6;

        _painelEntradasMouse = new PainelEntradasMouse
        {
            Location = new Point(margem, y),
            Size = new Size(largura, 66),
        };
        Controls.Add(_painelEntradasMouse);
        y += 66 + 18;

        // Sensibilidade
        Controls.Add(NovoRotulo("SENSIBILIDADE", margem, y, largura, 18, secao, Tema.Suave, Tema.Fundo));
        y += 22;

        var cartaoSens = new PainelCartao
        {
            Location = new Point(margem, y),
            Size = new Size(largura, 172),
        };
        Controls.Add(cartaoSens);

        cartaoSens.Controls.Add(NovoRotulo(
            "Curso total do volante", 16, 12, 240, 22, Font, Tema.Texto, Tema.FundoCartao));

        _lblCurso = NovoRotulo(
            "", largura - 16 - 100, 12, 100, 22, negrito, Tema.Destaque, Tema.FundoCartao,
            ContentAlignment.MiddleRight);
        cartaoSens.Controls.Add(_lblCurso);

        cartaoSens.Controls.Add(NovoRotulo(
            "Curso total de ponta a ponta (esquerda + direita).",
            16, 36, largura - 32, 18, pequeno, Tema.Suave, Tema.FundoCartao));

        // Cada passo = 10°. 9 a 180 equivale a 90° até 1800° no total.
        _trackCurso = new TrackBar
        {
            Minimum = 9,
            Maximum = 180,
            SmallChange = 1,
            LargeChange = 9,
            TickStyle = TickStyle.None,
            AutoSize = false,
            Location = new Point(10, 56),
            Size = new Size(largura - 20, 30),
            BackColor = Tema.FundoCartao,
        };
        cartaoSens.Controls.Add(_trackCurso);

        _lblVoltasCurso = NovoRotulo(
            "", 16, 86, largura - 32, 22, negrito, Tema.Destaque, Tema.FundoCartao);
        cartaoSens.Controls.Add(_lblVoltasCurso);

        _chkInverter = NovoCheck("Inverter sentido", 16, 117);
        cartaoSens.Controls.Add(_chkInverter);

        _chkMeio = NovoCheck("Botão do meio recentraliza (em vez de ser o botão 3)", 16, 143);
        cartaoSens.Controls.Add(_chkMeio);

        y += 172 + 18;

        // Calibração
        Controls.Add(NovoRotulo("CALIBRAÇÃO DO SENSOR", margem, y, largura, 18, secao, Tema.Suave, Tema.Fundo));
        y += 22;

        var cartaoCal = new PainelCartao
        {
            Location = new Point(margem, y),
            Size = new Size(largura, 190),
        };
        Controls.Add(cartaoCal);

        cartaoCal.Controls.Add(NovoRotulo(
            "Giro por movimento do mouse", 16, 14, 230, 22, Font, Tema.Texto, Tema.FundoCartao));

        _numGraus = new NumericUpDown
        {
            DecimalPlaces = 5,
            Minimum = 0.001m,
            Maximum = 1m,
            Increment = 0.0005m,
            Location = new Point(largura - 16 - 110, 12),
            Size = new Size(110, 26),
            BackColor = Tema.Fundo,
            ForeColor = Tema.Texto,
            BorderStyle = BorderStyle.FixedSingle,
        };
        cartaoCal.Controls.Add(_numGraus);

        cartaoCal.Controls.Add(NovoRotulo(
            "Ajusta quanto o volante virtual gira quando você move o mouse.\nDe 0,001° a 1° por pequeno deslocamento; valor maior = mais giro.",
            16, 42, largura - 32, 32, pequeno, Tema.Suave, Tema.FundoCartao));

        cartaoCal.Controls.Add(new Panel
        {
            Location = new Point(16, 84),
            Size = new Size(largura - 32, 1),
            BackColor = Tema.Borda,
        });

        cartaoCal.Controls.Add(NovoRotulo(
            "Voltas completas", 16, 95, 120, 22, Font, Tema.Texto, Tema.FundoCartao));

        _lblVoltas = NovoRotulo(
            "1", 140, 95, 60, 22, negrito, Tema.Destaque, Tema.FundoCartao,
            ContentAlignment.MiddleRight);
        cartaoCal.Controls.Add(_lblVoltas);

        _btnMedir = NovoBotao("Iniciar medição", largura - 16 - 170, 91, 170, 30, false);
        cartaoCal.Controls.Add(_btnMedir);

        _lblMedicao = NovoRotulo(
            "Gire o disco só num sentido, de volta à marca inicial.",
            16, 124, largura - 32, 18, pequeno, Tema.Suave, Tema.FundoCartao);
        cartaoCal.Controls.Add(_lblMedicao);

        _trackVoltas = new TrackBar
        {
            Minimum = 2,
            Maximum = 20,
            Value = 2,
            SmallChange = 1,
            LargeChange = 2,
            TickStyle = TickStyle.None,
            AutoSize = false,
            Location = new Point(10, 146),
            Size = new Size(largura - 20, 30),
            BackColor = Tema.FundoCartao,
        };
        cartaoCal.Controls.Add(_trackVoltas);

        y += 190 + 18;

        // Ações
        var btnPadroes = NovoBotao("Restaurar ajustes", margem, y, largura, 38, false);
        btnPadroes.Click += (s, e) => RestaurarAjustes();
        Controls.Add(btnPadroes);
        y += 38 + 12;

        var btnDiagnostico = NovoBotao("Diagnóstico...", margem, y, 150, 30, false);
        btnDiagnostico.Click += (s, e) => AbrirJanelaDiagnostico();
        Controls.Add(btnDiagnostico);
        y += 30 + 8;

        _lblMensagem = NovoRotulo("", margem, y, largura, 34, pequeno, Tema.Suave, Tema.Fundo);
        Controls.Add(_lblMensagem);
        y += 34 + 10;

        ClientSize = new Size(margem * 2 + largura, y);

        ResumeLayout(false);
    }

    void AplicarConfiguracaoNaTela()
    {
        // Ainda sem eventos ligados, então nada daqui dispara salvamento.
        int passos = (int)Math.Round(_cfg.LimiteGraus * 2.0 / 10.0);
        _trackCurso.Value = Math.Max(_trackCurso.Minimum, Math.Min(_trackCurso.Maximum, passos));
        _cfg.LimiteGraus = _trackCurso.Value * 5.0;
        AtualizarResumoCurso();

        _chkInverter.Checked = _cfg.InverterSentido;
        _chkMeio.Checked = _cfg.BotaoMeioRecentraliza;

        _numGraus.Value = LimitarGraus((decimal)_cfg.GrausPorContagem);
        _cfg.GrausPorContagem = (double)_numGraus.Value;
    }

    void LigarEventos()
    {
        _trackCurso.ValueChanged += (s, e) =>
        {
            int total = _trackCurso.Value * 10;

            AtualizarResumoCurso();
            _cfg.LimiteGraus = total / 2.0;
            AoMudarConfiguracao();
        };

        _chkInverter.CheckedChanged += (s, e) =>
        {
            _cfg.InverterSentido = _chkInverter.Checked;
            AoMudarConfiguracao();
        };

        _chkMeio.CheckedChanged += (s, e) =>
        {
            _cfg.BotaoMeioRecentraliza = _chkMeio.Checked;
            AoMudarConfiguracao();
        };

        _numGraus.ValueChanged += (s, e) =>
        {
            _cfg.GrausPorContagem = (double)_numGraus.Value;
            AoMudarConfiguracao();
        };

        _trackVoltas.ValueChanged += (s, e) =>
        {
            _lblVoltas.Text = VoltasSelecionadas.ToString("0.#");
        };

        _btnMedir.Click += (s, e) => AlternarMedicao();
        _btnReconectar.Click += (s, e) => Conectar();

        _timerTela.Tick += (s, e) => AtualizarTela();

        _timerSalvar.Tick += (s, e) =>
        {
            _timerSalvar.Stop();
            SalvarConfiguracao(false);
        };

        // Chamados pela thread do motor: passam para a thread da tela.
        _timerAvisoRecentralizacao.Tick += (s, e) =>
        {
            _timerAvisoRecentralizacao.Stop();
            _lblAvisoRecentralizacao.Text = "";
        };

        _motor.Mensagem += texto => NaTela(() => MostrarMensagemMotor(texto));
        _motor.Parou += () => NaTela(AtualizarStatus);

        Shown += (s, e) => Conectar();

        FormClosing += (s, e) =>
        {
            _timerTela.Stop();
            _timerSalvar.Stop();
            _timerAvisoRecentralizacao.Stop();
            _timerAvisoRecentralizacao.Dispose();

            SalvarConfiguracao(true);
            _motor.Dispose();
            _janelaDiagnostico.Dispose();
        };
    }

    // ---------------- Ações ----------------

    void Conectar()
    {
        try
        {
            _motor.Iniciar();
        }
        catch (Exception erro)
        {
            _lblMensagem.ForeColor = Tema.Erro;
            _lblMensagem.Text = erro.Message;
        }

        AtualizarStatus();
        _timerTela.Start();
    }

    void AbrirJanelaDiagnostico()
    {
        if (_janelaDiagnostico.IsDisposed)
            _janelaDiagnostico = new JanelaDiagnostico(_cfg, _motor, ProgramarSalvamento);

        if (!_janelaDiagnostico.Visible)
            _janelaDiagnostico.Show(this);

        _janelaDiagnostico.BringToFront();
    }

    void AoMudarConfiguracao()
    {
        _motor.ConfiguracaoAlterada();

        ProgramarSalvamento();
    }

    void ProgramarSalvamento()
    {
        // Salva meio segundo depois da última mudança (não a cada passo do controle).
        _timerSalvar.Stop();
        _timerSalvar.Start();
    }

    void SalvarConfiguracao(bool exibirErro)
    {
        try
        {
            ArmazenamentoConfiguracao.Salvar(_cfg);

            if (_lblMensagem.Text == _erroSalvar)
            {
                _lblMensagem.Text = "";
                _lblMensagem.ForeColor = Tema.Suave;
            }

            _erroSalvar = null;
        }
        catch (Exception erro) when (ErroDePersistencia(erro))
        {
            _erroSalvar = "Não foi possível salvar as configurações: " + erro.Message;
            _lblMensagem.Text = _erroSalvar;
            _lblMensagem.ForeColor = Tema.Erro;

            if (exibirErro)
            {
                MessageBox.Show(
                    this,
                    _erroSalvar,
                    "Erro ao salvar as configurações",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
    }

    static bool ErroDePersistencia(Exception erro)
    {
        return erro is IOException ||
            erro is InvalidDataException ||
            erro is UnauthorizedAccessException ||
            erro is SecurityException ||
            erro is JsonException;
    }

    void RestaurarAjustes()
    {
        var padrao = new Configuracao();

        int passos = (int)Math.Round(padrao.LimiteGraus * 2.0 / 10.0);
        _trackCurso.Value = Math.Max(_trackCurso.Minimum, Math.Min(_trackCurso.Maximum, passos));

        _chkInverter.Checked = padrao.InverterSentido;
        _chkMeio.Checked = padrao.BotaoMeioRecentraliza;
    }

    void AlternarMedicao()
    {
        if (!_medindo)
        {
            if (!_motor.Rodando)
            {
                _lblMedicao.Text = "Conecte o volante antes de medir.";
                return;
            }

            _motor.IniciarMedicao();
            _medindo = true;

            _trackVoltas.Enabled = false;
            _btnMedir.Text = "Concluir medição";
            _lblMedicao.Text =
                $"Gire exatamente {VoltasSelecionadas:0.#} volta(s), só num sentido, e clique em Concluir.";
            return;
        }

        long bruto = _motor.PararMedicao();
        long total = Math.Abs(bruto);
        decimal voltas = VoltasSelecionadas;

        _medindo = false;
        _trackVoltas.Enabled = true;
        _btnMedir.Text = "Iniciar medição";

        if (total < 100)
        {
            _lblMedicao.Text = "Quase nenhum movimento medido. Tente de novo.";
            return;
        }

        double graus = 360.0 * (double)voltas / total;
        decimal aplicado = LimitarGraus(Math.Round((decimal)graus, 5));

        _numGraus.Value = aplicado;

        _lblMedicao.Text =
            $"{aplicado}° por passo do mouse; {total:N0} passos em {voltas:0.#} voltas.";
    }

    decimal VoltasSelecionadas => _trackVoltas.Value / 2m;

    void AtualizarResumoCurso()
    {
        int cursoTotal = _trackCurso.Value * 10;
        _lblCurso.Text = cursoTotal + "°";
        _lblVoltasCurso.Text = $"VOLTAS POR LADO: {cursoTotal / 720.0:0.###}";
    }

    // ---------------- Atualização da tela ----------------

    void AtualizarTela()
    {
        bool rodando = _motor.Rodando;

        if (rodando != _ultimoRodando)
        {
            _ultimoRodando = rodando;
            AtualizarStatus();
        }

        _painelPosicao.Atualizar(_motor.Graus, _motor.Proporcao, _cfg.LimiteGraus, rodando);
        _painelEntradasMouse.Atualizar(_motor.EntradasMouse, rodando);
    }

    void AtualizarStatus()
    {
        bool rodando = _motor.Rodando;

        _ultimoRodando = rodando;
        _lblStatus.Text = rodando ? "●  Conectado" : "●  Desconectado";
        _lblStatus.ForeColor = rodando ? Tema.Ok : Tema.Erro;
        _btnReconectar.Visible = !rodando;

        _painelPosicao.Atualizar(_motor.Graus, _motor.Proporcao, _cfg.LimiteGraus, rodando);
        _painelEntradasMouse.Atualizar(_motor.EntradasMouse, rodando);
    }

    // ---------------- Auxiliares ----------------

    void NaTela(Action acao)
    {
        if (IsDisposed || !IsHandleCreated)
            return;

        try
        {
            BeginInvoke(acao);
        }
        catch (InvalidOperationException)
        {
            // A janela está fechando.
        }
    }

    static decimal LimitarGraus(decimal valor)
    {
        return Math.Min(1m, Math.Max(0.001m, valor));
    }

    static Label NovoRotulo(
        string texto, int x, int y, int largura, int altura,
        Font fonte, Color cor, Color fundo,
        ContentAlignment alinhamento = ContentAlignment.MiddleLeft)
    {
        return new Label
        {
            Text = texto,
            Location = new Point(x, y),
            Size = new Size(largura, altura),
            Font = fonte,
            ForeColor = cor,
            BackColor = fundo,
            AutoSize = false,
            TextAlign = alinhamento,
        };
    }

    static CheckBox NovoCheck(string texto, int x, int y)
    {
        return new CheckBox
        {
            Text = texto,
            Location = new Point(x, y),
            AutoSize = true,
            ForeColor = Tema.Texto,
            BackColor = Tema.FundoCartao,
        };
    }

    static Button NovoBotao(string texto, int x, int y, int largura, int altura, bool destaque)
    {
        var botao = new Button
        {
            Text = texto,
            Location = new Point(x, y),
            Size = new Size(largura, altura),
            FlatStyle = FlatStyle.Flat,
            BackColor = destaque ? Tema.Destaque : Tema.FundoCartao,
            ForeColor = destaque ? Color.White : Tema.Texto,
            Cursor = Cursors.Hand,
            UseVisualStyleBackColor = false,
        };

        botao.FlatAppearance.BorderColor = destaque ? Tema.Destaque : Tema.Borda;
        botao.FlatAppearance.MouseOverBackColor =
            destaque ? Color.FromArgb(58, 126, 230) : Color.FromArgb(238, 241, 245);

        return botao;
    }

    // Barra de título clara (Windows 10 20H1 ou mais novo; nos outros é ignorado).
    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);

        int temaEscuro = 0;

        if (DwmSetWindowAttribute(Handle, 20, ref temaEscuro, sizeof(int)) != 0)
            DwmSetWindowAttribute(Handle, 19, ref temaEscuro, sizeof(int));
    }
}