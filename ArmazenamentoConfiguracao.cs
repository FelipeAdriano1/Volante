using System;
using System.IO;
using System.Text.Json;

namespace Volante;

/// <summary>Guarda e lê as configurações em %LocalAppData%\Volante\configuracao.json.</summary>
static class ArmazenamentoConfiguracao
{
    static readonly JsonSerializerOptions Opcoes = new JsonSerializerOptions { WriteIndented = true };

    static string Pasta =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Volante");

    static string Arquivo => Path.Combine(Pasta, "configuracao.json");

    static string ArquivoLegado =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Volante",
            "config.json");

    public static Configuracao Carregar()
    {
        string json = LerArquivoSeExistir(Arquivo) ?? LerArquivoSeExistir(ArquivoLegado);

        if (json == null)
            return new Configuracao();

        Configuracao cfg = JsonSerializer.Deserialize<Configuracao>(json, Opcoes);

        if (cfg == null)
            throw new InvalidDataException("O arquivo de configurações não contém um objeto válido.");

        Validar(cfg);
        return cfg;
    }

    public static void Salvar(Configuracao cfg)
    {
        if (cfg == null)
            throw new ArgumentNullException(nameof(cfg));

        Validar(cfg);
        Directory.CreateDirectory(Pasta);

        string temporario = Arquivo + "." + Guid.NewGuid().ToString("N") + ".tmp";
        File.WriteAllText(temporario, JsonSerializer.Serialize(cfg, Opcoes));
        File.Move(temporario, Arquivo, true);
    }

    static string LerArquivoSeExistir(string caminho)
    {
        try
        {
            return File.ReadAllText(caminho);
        }
        catch (FileNotFoundException)
        {
            return null;
        }
        catch (DirectoryNotFoundException)
        {
            return null;
        }
    }

    static void Validar(Configuracao cfg)
    {
        if (!double.IsFinite(cfg.GrausPorContagem) ||
            cfg.GrausPorContagem < 0.001 ||
            cfg.GrausPorContagem > 1.0)
            throw new InvalidDataException("Graus por movimento deve estar entre 0,001 e 1.");

        if (!double.IsFinite(cfg.LimiteGraus) ||
            cfg.LimiteGraus < 45.0 ||
            cfg.LimiteGraus > 900.0)
            throw new InvalidDataException("O curso por lado deve estar entre 45° e 900°.");

        if (string.IsNullOrWhiteSpace(cfg.HardwareIdMouse))
            throw new InvalidDataException("O identificador do mouse não pode estar vazio.");

        if (cfg.VJoyId == 0 || cfg.VJoyId > 16)
            throw new InvalidDataException("O identificador do vJoy deve estar entre 1 e 16.");

        if (cfg.TeclaRecentralizar < 1 || cfg.TeclaRecentralizar > 255 ||
            cfg.TeclaContagem < 1 || cfg.TeclaContagem > 255)
            throw new InvalidDataException("Os códigos das teclas devem estar entre 1 e 255.");

        if (cfg.BotaoRodaCima < 4 || cfg.BotaoRodaCima > 128 ||
            cfg.BotaoRodaBaixo < 4 || cfg.BotaoRodaBaixo > 128 ||
            cfg.BotaoRodaCima == cfg.BotaoRodaBaixo)
            throw new InvalidDataException("Os botões da roda devem ser diferentes e estar entre 4 e 128.");

        if (cfg.PulsoRodaMs <= 0)
            throw new InvalidDataException("A duração do pulso da roda deve ser maior que zero.");
    }
}