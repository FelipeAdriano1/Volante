namespace Volante;

/// <summary>
/// Todas as opções do volante num só lugar.
/// Algumas opções podem ser alteradas com o motor rodando; dispositivos são definidos ao iniciar.
/// </summary>
public sealed class Configuracao
{
    // ----- Dispositivos -----
    public string HardwareIdMouse { get; set; } = "VID_30FA&PID_0302";
    public uint VJoyId { get; set; } = 1;

    // ----- Volante -----
    public double GrausPorContagem { get; set; } = 0.03252;

    /// <summary>Limite para cada lado. 900° = 1800° no total (batente a batente).</summary>
    public double LimiteGraus { get; set; } = 900.0;

    /// <summary>Troque para true se o carro virar ao contrário no jogo.</summary>
    public bool InverterSentido { get; set; } = false;

    // ----- Teclas globais (códigos virtual-key) -----
    /// <summary>0x78 = F9.</summary>
    public int TeclaRecentralizar { get; set; } = 0x78;

    /// <summary>0x79 = F10 (só com Diagnostico = true).</summary>
    public int TeclaContagem { get; set; } = 0x79;

    /// <summary>Se true, o botão do meio recentraliza em vez de ser o botão 3 do vJoy.</summary>
    public bool BotaoMeioRecentraliza { get; set; } = false;

    // ----- Roda do mouse (pulso curto em botões do vJoy) -----
    public byte BotaoRodaCima { get; set; } = 4;
    public byte BotaoRodaBaixo { get; set; } = 5;
    public int PulsoRodaMs { get; set; } = 50;

    // ----- Diagnóstico -----
    /// <summary>
    /// Publica estatísticas de entrada a cada 1 s, pausas suspeitas do sensor
    /// e habilita o teste de contagem (F10).
    /// </summary>
    public bool Diagnostico { get; set; } = false;
}
