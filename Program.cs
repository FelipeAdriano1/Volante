using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Security;
using System.Text.Json;
using System.Windows.Forms;

namespace Volante;

static class Program
{
    [STAThread]
    static void Main()
    {
        Application.SetHighDpiMode(HighDpiMode.SystemAware);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        try
        {
            using (Process processo = Process.GetCurrentProcess())
                processo.PriorityClass = ProcessPriorityClass.High;
        }
        catch (Win32Exception)
        {
            // A prioridade é opcional; o programa segue com a prioridade normal.
        }
        catch (InvalidOperationException)
        {
            // O processo pode já estar encerrando.
        }

        Configuracao config;
        try
        {
            config = ArmazenamentoConfiguracao.Carregar();
        }
        catch (Exception erro) when (
            erro is IOException ||
            erro is InvalidDataException ||
            erro is UnauthorizedAccessException ||
            erro is SecurityException ||
            erro is JsonException)
        {
            MessageBox.Show(
                "Não foi possível carregar as configurações. O arquivo original foi mantido.\n\n" + erro.Message,
                "Erro nas configurações",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return;
        }

        using (var motor = new MotorVolante(config))
        {
            Application.Run(new JanelaPrincipal(config, motor));
        }
    }
}
