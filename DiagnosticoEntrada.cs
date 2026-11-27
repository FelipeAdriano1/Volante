using System;
using System.Diagnostics;

namespace Volante;

/// <summary>
/// Ferramentas para investigar o sensor e a entrada. Só é usada com Configuracao.Diagnostico = true.
/// Roda inteira na thread do motor (não precisa de lock).
/// </summary>
sealed class DiagnosticoEntrada
{
    readonly Action<string> _publicar;

    long _ultimoRelatorio = Environment.TickCount64;
    long _pico;
    int _maiorLote;
    int _eventos;

    long _ultimoTs;
    int _ultimoDy;

    bool _contando;
    long _contagem;

    public DiagnosticoEntrada(Action<string> publicar)
    {
        _publicar = publicar;
    }

    public void RegistrarLote(int quantidade)
    {
        _eventos += quantidade;

        if (quantidade > _maiorLote)
            _maiorLote = quantidade;
    }

    /// <summary>Chamado a cada movimento no eixo Y (valor bruto, sem inversão).</summary>
    public void RegistrarMovimento(int dy)
    {
        long modulo = Math.Abs((long)dy);

        if (modulo > _pico)
            _pico = modulo;

        if (_contando)
            _contagem += dy;

        long ts = Stopwatch.GetTimestamp();
        double pausaMs = (ts - _ultimoTs) * 1000.0 / Stopwatch.Frequency;

        // Pausa sem eventos logo depois de movimento rápido.
        if (_ultimoTs != 0 && Math.Abs((long)_ultimoDy) >= 5 && pausaMs > 30 && pausaMs < 500)
        {
            string inverteu = Math.Sign(_ultimoDy) != Math.Sign(dy) ? "INVERTEU" : "mesmo sentido";

            _publicar(
                $"[diag] pausa de {pausaMs:F0} ms apos dy={_ultimoDy}; voltou com dy={dy} ({inverteu})");
        }

        _ultimoTs = ts;
        _ultimoDy = dy;
    }

    /// <summary>Primeira chamada inicia a contagem; a segunda mostra o total.</summary>
    public void AlternarContagem()
    {
        if (!_contando)
        {
            _contagem = 0;
            _contando = true;
            _publicar("[contagem] INICIADA. Gire e aperte a tecla de novo para ver o total.");
        }
        else
        {
            _contando = false;
            _publicar($"[contagem] TOTAL = {_contagem} contagens");
        }
    }

    public void Desativar()
    {
        if (!_contando)
            return;

        _contando = false;
        _publicar($"[contagem] TOTAL = {_contagem} contagens (diagnóstico desativado)");
    }

    /// <summary>Imprime um resumo a cada 1 s.</summary>
    public void Relatorio(long agora)
    {
        if (agora - _ultimoRelatorio < 1000)
            return;

        _publicar($"[diag] eventos/s={_eventos} pico|dy|={_pico} maiorLote={_maiorLote}");

        _ultimoRelatorio = agora;
        _eventos = 0;
        _pico = 0;
        _maiorLote = 0;
    }
}
