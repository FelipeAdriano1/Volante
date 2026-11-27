using System;
using System.Drawing;
using System.Windows.Forms;

namespace Volante;

sealed class JanelaDiagnostico : Form
{
    const int LimiteLinhas = 500;

    readonly Configuracao _configuracao;
    readonly MotorVolante _motor;
    readonly Action _configuracaoAlterada;
    readonly CheckBox _chkDiagnostico;
    readonly RichTextBox _log;
    readonly Font _fonteTitulo = new Font("Segoe UI", 10F, FontStyle.Bold);

    public JanelaDiagnostico(
        Configuracao configuracao,
        MotorVolante motor,
        Action configuracaoAlterada)
    {
        _configuracao = configuracao;
        _motor = motor;
        _configuracaoAlterada = configuracaoAlterada;

        Text = "Diagnóstico do sensor";
        ClientSize = new Size(620, 420);
        MinimumSize = new Size(640, 480);
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = true;
        MinimizeBox = true;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Tema.Fundo;
        ForeColor = Tema.Texto;
        Font = SystemFonts.MessageBoxFont;

        Controls.Add(new Label
        {
            Text = "Diagnóstico do sensor",
            Location = new Point(20, 16),
            Size = new Size(580, 24),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            Font = _fonteTitulo,
            ForeColor = Tema.Texto,
            BackColor = Tema.Fundo,
        });

        Controls.Add(new Label
        {
            Text = "Os relatórios e avisos aparecem abaixo. Com o modo ativo, F10 inicia ou encerra a contagem bruta.",
            Location = new Point(20, 46),
            Size = new Size(580, 36),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            ForeColor = Tema.Suave,
            BackColor = Tema.Fundo,
        });

        _chkDiagnostico = new CheckBox
        {
            Text = "Ativar modo de diagnóstico",
            Location = new Point(20, 88),
            Size = new Size(580, 24),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            Checked = _configuracao.Diagnostico,
            ForeColor = Tema.Texto,
            BackColor = Tema.Fundo,
            UseVisualStyleBackColor = false,
        };
        _chkDiagnostico.CheckedChanged += AoMudarDiagnostico;
        Controls.Add(_chkDiagnostico);

        _log = new RichTextBox
        {
            Location = new Point(20, 120),
            Size = new Size(580, 240),
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            ReadOnly = true,
            WordWrap = true,
            DetectUrls = false,
            BackColor = Tema.FundoCartao,
            ForeColor = Tema.Texto,
            BorderStyle = BorderStyle.FixedSingle,
        };
        _log.Text = "Ative o diagnóstico para começar a receber relatórios.";
        Controls.Add(_log);

        var btnLimpar = CriarBotao("Limpar", new Point(420, 374), new Size(85, 28));
        btnLimpar.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        btnLimpar.Click += (s, e) => _log.Clear();
        Controls.Add(btnLimpar);

        var btnOcultar = CriarBotao("Ocultar", new Point(515, 374), new Size(85, 28));
        btnOcultar.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        btnOcultar.Click += (s, e) => Hide();
        Controls.Add(btnOcultar);

        FormClosing += AoFechar;
        _motor.DiagnosticoMensagem += AoReceberMensagem;

        // A janela começa oculta, mas precisa receber mensagens desde a inicialização do motor.
        _ = Handle;
    }

    void AoMudarDiagnostico(object sender, EventArgs e)
    {
        _configuracao.Diagnostico = _chkDiagnostico.Checked;
        _motor.DiagnosticoAlterado();
        _configuracaoAlterada();

        AdicionarMensagem(_chkDiagnostico.Checked
            ? "Diagnóstico ativado."
            : "Diagnóstico desativado.");
    }

    void AoReceberMensagem(string mensagem)
    {
        if (IsDisposed)
            return;

        try
        {
            BeginInvoke(new Action(() => AdicionarMensagem(mensagem)));
        }
        catch (InvalidOperationException)
        {
            // A janela está sendo encerrada.
        }
    }

    void AdicionarMensagem(string mensagem)
    {
        if (_log.Text == "Ative o diagnóstico para começar a receber relatórios.")
            _log.Clear();

        _log.AppendText($"{DateTime.Now:HH:mm:ss}  {mensagem}{Environment.NewLine}");

        int linhas = _log.Lines.Length;
        if (linhas > LimiteLinhas)
        {
            int inicio = _log.GetFirstCharIndexFromLine(linhas - LimiteLinhas);
            if (inicio > 0)
            {
                _log.Select(0, inicio);
                _log.SelectedText = string.Empty;
            }
        }

        _log.SelectionStart = _log.TextLength;
        _log.ScrollToCaret();
    }

    void AoFechar(object sender, FormClosingEventArgs e)
    {
        if (e.CloseReason != CloseReason.UserClosing)
            return;

        e.Cancel = true;
        Hide();
    }

    static Button CriarBotao(string texto, Point localizacao, Size tamanho)
    {
        var botao = new Button
        {
            Text = texto,
            Location = localizacao,
            Size = tamanho,
            FlatStyle = FlatStyle.Flat,
            BackColor = Tema.FundoCartao,
            ForeColor = Tema.Texto,
            UseVisualStyleBackColor = false,
        };
        botao.FlatAppearance.BorderColor = Tema.Borda;
        botao.FlatAppearance.MouseOverBackColor = Color.FromArgb(238, 241, 245);
        return botao;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _motor.DiagnosticoMensagem -= AoReceberMensagem;
            _fonteTitulo.Dispose();
        }

        base.Dispose(disposing);
    }
}
