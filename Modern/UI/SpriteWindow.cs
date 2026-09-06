using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Scmpoo.Modern.Animation;
using Scmpoo.Modern.Rendering;

namespace Scmpoo.Modern.UI;

public sealed class SpriteWindow : Form
{
    private const int WsExToolWindow = 0x80;
    private const int WsExNoActivate = 0x08000000;
    private const uint SwpNoActivate = 0x0010;
    private const uint SwpNoZOrder = 0x0004;
    private const uint SwpNoSize = 0x0001;
    private readonly SpriteAtlas atlas;
    private readonly bool companion;
    private readonly Action<Point> drag;
    private readonly Action endDrag;
    private readonly Action openSettings;
    private readonly Action close;
    private readonly Action<string> dropFile;
    private readonly ContextMenuStrip menu = new();
    private readonly Timer menuTimer = new();
    private Bitmap? bitmap;
    private Bitmap? beamSurface;
    private int[]? beamPixels;
    private int spriteIndex = -1;
    private int spriteScale;
    private int fadeStep = -1;
    private int beamHeight;
    private bool alwaysOnTop;
    private bool dragging;
    private bool suppressRightUp;
    private Point dragPointer;
    private Point dragOrigin;
    private Point menuPoint;

    public SpriteWindow(SpriteAtlas atlas, bool companion, Action<Point> drag, Action endDrag,
        Action openSettings, Action<SheepAction> action, Action close, Action fill,
        Action closeAll, Action<string> dropFile)
    {
        this.atlas = atlas ?? throw new ArgumentNullException(nameof(atlas));
        this.companion = companion;
        this.drag = drag;
        this.endDrag = endDrag;
        this.openSettings = openSettings;
        this.close = close;
        this.dropFile = dropFile;
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        AutoScaleMode = AutoScaleMode.None;
        BackColor = Color.Black;
        Size = new Size(40, 40);
        Text = companion ? "Scmpoo Companion" : "Scmpoo Modern";
        AllowDrop = !companion;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        menu.Items.Add("小羊设置...", null, (_, _) => openSettings?.Invoke());
        menu.Items.Add(new ToolStripSeparator());
        AddAction("叫一声", SheepAction.Baa, action);
        AddAction("喂一朵花", SheepAction.Flower, action);
        AddAction("燃烧与浴盆", SheepAction.Bathtub, action);
        AddAction("遇见黑羊", SheepAction.BlackSheep, action);
        AddAction("UFO 事件", SheepAction.Ufo, action);
        AddAction("从空中落下", SheepAction.Fall, action);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("一键开启 32 只小羊", null, (_, _) => fill?.Invoke());
        menu.Items.Add("退出这只小羊", null, (_, _) => close?.Invoke());
        menu.Items.Add("关闭所有小羊", null, (_, _) => closeAll?.Invoke());
        menuTimer.Interval = SystemInformation.DoubleClickTime;
        menuTimer.Tick += (_, _) =>
        {
            menuTimer.Stop();
            if (!IsDisposed && Visible) menu.Show(menuPoint);
        };
    }

    public Point LogicalPosition { get; private set; }
    protected override bool ShowWithoutActivation => true;

    protected override CreateParams CreateParams
    {
        get
        {
            CreateParams parameters = base.CreateParams;
            parameters.ExStyle |= WsExToolWindow | WsExNoActivate;
            return parameters;
        }
    }

    private void AddAction(string text, SheepAction value, Action<SheepAction> action)
        => menu.Items.Add(text, null, (_, _) => action?.Invoke(value));

    public void Present(SpriteFrame frame, int scale, bool alwaysOnTop)
    {
        if (IsDisposed) return;
        LogicalPosition = new Point(frame.X, frame.Y);
        if (!frame.Visible)
        {
            if (Visible) Hide();
            return;
        }
        int size = SpriteAtlas.SpriteSize * scale;
        int nextBeamHeight = checked(frame.BeamHeight * scale);
        int x = frame.X - (size - SpriteAtlas.SpriteSize) / 2;
        int y = frame.Y - (size - SpriteAtlas.SpriteSize);
        bool shapeChanged = spriteIndex != frame.SpriteIndex || spriteScale != scale ||
            fadeStep != frame.FadeStep || beamHeight != nextBeamHeight;
        if (shapeChanged)
        {
            bitmap = atlas.GetBitmap(frame.SpriteIndex, scale, frame.FadeStep, frame.BathtubOverlay);
            Region? previous = Region;
            Region = atlas.CreateRegion(frame.SpriteIndex, scale, frame.FadeStep, frame.BathtubOverlay, frame.BeamHeight);
            previous?.Dispose();
            spriteIndex = frame.SpriteIndex;
            spriteScale = scale;
            fadeStep = frame.FadeStep;
            beamHeight = nextBeamHeight;
        }
        IntPtr insertAfter = alwaysOnTop ? new IntPtr(-1) : new IntPtr(-2);
        uint flags = SwpNoActivate;
        if (this.alwaysOnTop == alwaysOnTop && Visible) flags |= SwpNoZOrder;
        if (Width == size && Height == size + beamHeight) flags |= SwpNoSize;
        if (Left != x || Top != y || shapeChanged || this.alwaysOnTop != alwaysOnTop || !Visible)
            SetWindowPos(Handle, insertAfter, x, y, size, size + beamHeight, flags);
        this.alwaysOnTop = alwaysOnTop;
        if (!Visible) Show();
        if (shapeChanged || beamHeight > 0) Invalidate();
    }

    protected override void OnPaintBackground(PaintEventArgs e) { }

    protected override void OnPaint(PaintEventArgs e)
    {
        if (bitmap is null) return;
        e.Graphics.DrawImageUnscaled(bitmap, 0, 0);
        if (beamHeight > 0) PaintBeam(e.Graphics, bitmap);
    }

    private void PaintBeam(Graphics target, Bitmap sprite)
    {
        int width = sprite.Width;
        if (beamSurface is null || beamPixels is null || beamSurface.Width != width || beamSurface.Height < beamHeight)
        {
            beamSurface?.Dispose();
            int capacity = ((beamHeight + 127) / 128) * 128;
            beamSurface = new Bitmap(width, capacity, PixelFormat.Format32bppArgb);
            beamPixels = new int[width * capacity];
        }
        try
        {
            using (Graphics capture = Graphics.FromImage(beamSurface))
                capture.CopyFromScreen(Left, Top + sprite.Height, 0, 0, new Size(width, beamHeight));
            BitmapData data = beamSurface.LockBits(new Rectangle(0, 0, width, beamHeight),
                ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            try
            {
                // Original beam operation: preserve red/green low bits and set their high bits.
                for (int y = 0; y < beamHeight; y++)
                {
                    IntPtr row = new(data.Scan0.ToInt64() + (long)y * data.Stride);
                    Marshal.Copy(row, beamPixels, y * width, width);
                    for (int x = 0; x < width; x++)
                        beamPixels[y * width + x] = (beamPixels[y * width + x] & 0x00ffff00) | unchecked((int)0xff808000);
                    Marshal.Copy(beamPixels, y * width, row, width);
                }
            }
            finally { beamSurface.UnlockBits(data); }
            target.DrawImage(beamSurface, new Rectangle(0, sprite.Height, width, beamHeight),
                new Rectangle(0, 0, width, beamHeight), GraphicsUnit.Pixel);
        }
        catch (System.ComponentModel.Win32Exception)
        {
            using Brush brush = new SolidBrush(Color.FromArgb(160, 180, 0));
            target.FillRectangle(brush, 0, sprite.Height, width, beamHeight);
        }
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (companion || e.Button != MouseButtons.Left) return;
        menuTimer.Stop();
        dragging = true;
        dragPointer = Cursor.Position;
        dragOrigin = LogicalPosition;
        Capture = true;
        drag?.Invoke(dragOrigin);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (!dragging) return;
        Point pointer = Cursor.Position;
        drag?.Invoke(new Point(dragOrigin.X + pointer.X - dragPointer.X, dragOrigin.Y + pointer.Y - dragPointer.Y));
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        if (companion) return;
        if (e.Button == MouseButtons.Left) FinishDrag();
        if (e.Button == MouseButtons.Right)
        {
            if (suppressRightUp) { suppressRightUp = false; return; }
            menuPoint = Cursor.Position;
            menuTimer.Stop();
            menuTimer.Start();
        }
    }

    protected override void OnMouseCaptureChanged(EventArgs e)
    {
        base.OnMouseCaptureChanged(e);
        if (!Capture) FinishDrag();
    }

    private void FinishDrag()
    {
        if (!dragging) return;
        dragging = false;
        Capture = false;
        endDrag?.Invoke();
    }

    protected override void OnMouseDoubleClick(MouseEventArgs e)
    {
        base.OnMouseDoubleClick(e);
        if (companion) return;
        menuTimer.Stop();
        if (e.Button == MouseButtons.Left)
        {
            FinishDrag();
            openSettings?.Invoke();
        }
        else if (e.Button == MouseButtons.Right)
        {
            suppressRightUp = true;
            close?.Invoke();
        }
    }

    protected override void OnDragEnter(DragEventArgs e)
    {
        base.OnDragEnter(e);
        if (!companion && e.Data?.GetDataPresent(DataFormats.FileDrop) == true) e.Effect = DragDropEffects.Copy;
    }

    protected override void OnDragDrop(DragEventArgs e)
    {
        base.OnDragDrop(e);
        if (companion) return;
        string[]? files = e.Data?.GetData(DataFormats.FileDrop) as string[];
        if (files != null && files.Length > 0) dropFile?.Invoke(files[0]);
    }

    protected override void WndProc(ref Message message)
    {
        if (message.Msg == 0x0021) { message.Result = new IntPtr(3); return; }
        if (companion && message.Msg == 0x0084) { message.Result = new IntPtr(-1); return; }
        base.WndProc(ref message);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            menuTimer.Dispose();
            menu.Dispose();
            beamSurface?.Dispose();
            beamSurface = null;
            Region? previous = Region;
            Region = null;
            previous?.Dispose();
        }
        base.Dispose(disposing);
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(IntPtr window, IntPtr insertAfter, int x, int y, int width, int height, uint flags);
}
