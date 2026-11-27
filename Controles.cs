using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Globalization;
using System.Windows.Forms;

namespace Volante;

static class Tema
{
    public static readonly Color Fundo = Color.White;
    public static readonly Color FundoCartao = Color.FromArgb(248, 249, 251);
    public static readonly Color Borda = Color.FromArgb(215, 220, 228);
    public static readonly Color Texto = Color.FromArgb(32, 35, 40);
    public static readonly Color Suave = Color.FromArgb(100, 108, 118);
    public static readonly Color Destaque = Color.FromArgb(42, 112, 220);
    public static readonly Color Ok = Color.FromArgb(32, 140, 76);
    public static readonly Color Erro = Color.FromArgb(190, 44, 44);
}

/// <summary>Painel claro com borda fina, usado para agrupar controles.</summary>
sealed class PainelCartao : Panel
{
    public PainelCartao()
    {
        DoubleBuffered = true;
        BackColor = Tema.FundoCartao;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        using (var caneta = new Pen(Tema.Borda))
            e.Graphics.DrawRectangle(caneta, 0, 0, Width - 1, Height - 1);
    }
}

/// <summary>Mostra os graus atuais e uma barra com a posição do volante.</summary>
sealed class PainelPosicao : Panel
{
    readonly Font _fonteGrande = new Font("Segoe UI", 26F, FontStyle.Bold);
    readonly Font _fontePequena = new Font("Segoe UI", 8F);

    double _graus;
    double _proporcao;
    double _limite = 450;
    bool _ativo;

    public PainelPosicao()
    {
        DoubleBuffered = true;
        BackColor = Tema.FundoCartao;
    }

    public void Atualizar(double graus, double proporcao, double limiteGraus, bool ativo)
    {
        if (Math.Abs(graus - _graus) < 0.05 &&
            Math.Abs(proporcao - _proporcao) < 0.0005 &&
            limiteGraus == _limite &&
            ativo == _ativo)
            return;

        _graus = graus;
        _proporcao = proporcao;
        _limite = limiteGraus;
        _ativo = ativo;

        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.Clear(Tema.FundoCartao);
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

        using (var caneta = new Pen(Tema.Borda))
            g.DrawRectangle(caneta, 0, 0, Width - 1, Height - 1);

        // Graus em destaque.
        string texto = _graus.ToString("+0.0;-0.0;0.0", CultureInfo.CurrentCulture) + "°";

        using (var pincel = new SolidBrush(_ativo ? Tema.Texto : Tema.Suave))
        using (var formato = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center,
        })
        {
            g.DrawString(texto, _fonteGrande, pincel, new RectangleF(0, 8, Width, Height - 52), formato);
        }

        // Barra de posição.
        int x0 = 24;
        int x1 = Width - 24;
        int y = Height - 36;
        int altura = 10;
        int centro = (x0 + x1) / 2;

        using (var trilho = new SolidBrush(Tema.Borda))
            g.FillRectangle(trilho, x0, y, x1 - x0, altura);

        double p = Math.Max(-1.0, Math.Min(1.0, _proporcao));
        int largura = (int)Math.Round(p * (x1 - x0) / 2.0);

        if (largura != 0)
        {
            using (var preenchimento = new SolidBrush(Tema.Destaque))
            {
                if (largura > 0)
                    g.FillRectangle(preenchimento, centro, y, largura, altura);
                else
                    g.FillRectangle(preenchimento, centro + largura, y, -largura, altura);
            }
        }

        using (var marca = new Pen(Tema.Texto, 2))
            g.DrawLine(marca, centro, y - 5, centro, y + altura + 5);

        // Extremos da barra.
        string esquerda = "-" + _limite.ToString("0", CultureInfo.CurrentCulture) + "°";
        string direita = "+" + _limite.ToString("0", CultureInfo.CurrentCulture) + "°";

        using (var suave = new SolidBrush(Tema.Suave))
        {
            g.DrawString(esquerda, _fontePequena, suave, x0 - 2, y + altura + 4);

            SizeF tamanho = g.MeasureString(direita, _fontePequena);
            g.DrawString(direita, _fontePequena, suave, x1 - tamanho.Width + 2, y + altura + 4);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _fonteGrande.Dispose();
            _fontePequena.Dispose();
        }

        base.Dispose(disposing);
    }
}

/// <summary>Indicadores de clique esquerdo/direito/meio e movimento da roda.</summary>
sealed class PainelEntradasMouse : Panel
{
    static readonly string[] Nomes =
    {
        "Clique\nesquerdo",
        "Clique\ndireito",
        "Clique do\nscroll",
        "Scroll para\ncima",
        "Scroll para\nbaixo",
    };

    readonly Font _fonteIndicador = new Font("Segoe UI", 9F, FontStyle.Bold);
    readonly Font _fonteNome = new Font("Segoe UI", 8F);

    int _mascara;
    bool _ativo;

    public PainelEntradasMouse()
    {
        DoubleBuffered = true;
        BackColor = Tema.FundoCartao;
    }

    public void Atualizar(int mascara, bool ativo)
    {
        if (mascara == _mascara && ativo == _ativo)
            return;

        _mascara = mascara;
        _ativo = ativo;

        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.Clear(Tema.FundoCartao);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

        using (var caneta = new Pen(Tema.Borda))
            g.DrawRectangle(caneta, 0, 0, Width - 1, Height - 1);

        float passo = (Width - 20f) / Nomes.Length;

        using (var formato = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center,
        })
        {
            for (int i = 0; i < Nomes.Length; i++)
            {
                float cx = 10f + passo * (i + 0.5f);
                var area = new RectangleF(cx - 12f, 5f, 24f, 24f);
                var nome = new RectangleF(cx - passo / 2f + 2f, 31f, passo - 4f, Height - 34f);

                bool ligado = _ativo && (_mascara & (1 << i)) != 0;

                using (var fundo = new SolidBrush(ligado ? Tema.Destaque : Tema.Borda))
                    g.FillEllipse(fundo, area);

                using (var texto = new SolidBrush(ligado ? Tema.Fundo : Tema.Suave))
                    g.DrawString(i < 3 ? "●" : (i == 3 ? "↑" : "↓"), _fonteIndicador, texto, area, formato);

                using (var texto = new SolidBrush(Tema.Suave))
                    g.DrawString(Nomes[i], _fonteNome, texto, nome, formato);
            }
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _fonteIndicador.Dispose();
            _fonteNome.Dispose();
        }

        base.Dispose(disposing);
    }
}