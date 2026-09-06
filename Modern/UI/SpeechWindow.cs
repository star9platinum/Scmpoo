using System;
using System.Drawing;
using System.Windows.Forms;

namespace Scmpoo.Modern.UI;

public sealed class SpeechWindow : Form
{
    private readonly Label message = new PassThroughLabel
    {
        UseMnemonic = false, AutoSize = false,
        BackColor = SystemColors.Info, ForeColor = SystemColors.InfoText
    };

    public SpeechWindow()
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        AutoScaleMode = AutoScaleMode.None;
        BackColor = SystemColors.Info;
        Font = SystemFonts.MessageBoxFont;
        message.Font = Font;
        Controls.Add(message);
    }

    protected override bool ShowWithoutActivation => true;

    protected override CreateParams CreateParams
    {
        get
        {
            CreateParams parameters = base.CreateParams;
            parameters.ExStyle |= 0x80 | 0x08000000;
            return parameters;
        }
    }

    public void ShowMessage(string text, Rectangle workArea, Point sheepLocation, bool topmost)
    {
        if (IsDisposed) return;
        Text = text;
        message.Text = text;
        int availableWidth = Math.Max(32, Math.Min(260, workArea.Width));
        Size measured = TextRenderer.MeasureText(text, Font,
            new Size(Math.Max(16, availableWidth - 16), int.MaxValue),
            TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix);
        ClientSize = new Size(Math.Min(availableWidth, measured.Width + 16), measured.Height + 12);
        message.Bounds = new Rectangle(8, 6, Math.Max(16, ClientSize.Width - 16), measured.Height);

        int x = sheepLocation.X - Width / 2;
        int y = sheepLocation.Y - Height - 8;
        if (y < workArea.Top)
        {
            x = sheepLocation.X + 28;
            y = sheepLocation.Y + 8;
        }
        Location = new Point(
            Math.Max(workArea.Left, Math.Min(x, workArea.Right - Width)),
            Math.Max(workArea.Top, Math.Min(y, workArea.Bottom - Height)));
        TopMost = topmost;
        if (!Visible) Show();
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        ControlPaint.DrawBorder(e.Graphics, ClientRectangle,
            SystemColors.InfoText, ButtonBorderStyle.Solid);
    }

    protected override void WndProc(ref Message message)
    {
        if (message.Msg == 0x0021) { message.Result = new IntPtr(3); return; }
        if (message.Msg == 0x0084) { message.Result = new IntPtr(-1); return; }
        base.WndProc(ref message);
    }

    private sealed class PassThroughLabel : Label
    {
        protected override void WndProc(ref Message message)
        {
            if (message.Msg == 0x0084) { message.Result = new IntPtr(-1); return; }
            base.WndProc(ref message);
        }
    }
}
