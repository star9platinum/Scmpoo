using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Scmpoo.Modern.Animation;

namespace Scmpoo.Modern.Platform;

internal static class Native
{
    internal const int NoActivate = 0x10, NoSize = 1, NoMove = 2;
    [StructLayout(LayoutKind.Sequential)] internal struct Rect { public int Left, Top, Right, Bottom; public Rectangle Bounds => Rectangle.FromLTRB(Left, Top, Right, Bottom); }
    internal delegate bool EnumWindow(IntPtr window, IntPtr parameter);
    [DllImport("user32.dll")] internal static extern bool EnumWindows(EnumWindow callback, IntPtr data);
    [DllImport("user32.dll")] internal static extern bool IsWindow(IntPtr window);
    [DllImport("user32.dll")] internal static extern bool IsWindowVisible(IntPtr window);
    [DllImport("user32.dll")] internal static extern bool IsIconic(IntPtr window);
    [DllImport("user32.dll")] internal static extern bool GetWindowRect(IntPtr window, out Rect rect);
    [DllImport("user32.dll")] internal static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);
    [DllImport("user32.dll")] internal static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] internal static extern IntPtr WindowFromPoint(Point point);
    [DllImport("user32.dll")] internal static extern bool SetWindowPos(IntPtr window, IntPtr after, int x, int y, int width, int height, uint flags);
    [DllImport("user32.dll", CharSet = CharSet.Auto)] internal static extern IntPtr FindWindow(string? className, string title);
    [DllImport("user32.dll", CharSet = CharSet.Auto)] internal static extern bool PostMessage(IntPtr window, uint message, IntPtr w, IntPtr l);
}

internal sealed class DesktopSnapshot
{
    private readonly uint processId = (uint)Process.GetCurrentProcess().Id;
    private long lastRefresh = -1000;
    public readonly List<DesktopWindow> Windows = new();
    public readonly List<Rectangle> Monitors = new();
    public Rectangle Bounds { get; private set; }
    public Point Pointer { get; private set; }
    public IntPtr Foreground { get; private set; }
    public int EnumerationCount { get; private set; }

    public void Refresh(long now, bool force = false)
    {
        Pointer = Cursor.Position;
        Foreground = Native.GetForegroundWindow();
        if (!force && now - lastRefresh < 250) return;
        lastRefresh = now;
        Windows.Clear();
        Native.EnumWindows((window, _) =>
        {
            Native.GetWindowThreadProcessId(window, out uint owner);
            if (owner != processId && Native.IsWindowVisible(window) && !Native.IsIconic(window) &&
                Native.GetWindowRect(window, out Native.Rect rect) && rect.Right > rect.Left && rect.Bottom > rect.Top)
                Windows.Add(new DesktopWindow(window, rect.Bounds));
            return Windows.Count < 128;
        }, IntPtr.Zero);
        EnumerationCount++;
        if (force || Monitors.Count == 0) RefreshMonitors();
    }

    public void RefreshMonitors()
    {
        Monitors.Clear();
        foreach (Screen screen in Screen.AllScreens) Monitors.Add(screen.WorkingArea);
        if (Monitors.Count == 0) Monitors.Add(new Rectangle(0, 0, 640, 480));
        Bounds = Monitors[0];
        foreach (Rectangle area in Monitors) Bounds = Rectangle.Union(Bounds, area);
        lastRefresh = -1000;
    }

    public Rectangle Nearest(Point point, int selected = -1)
    {
        if (selected >= 0 && selected < Monitors.Count) return Monitors[selected];
        Rectangle nearest = Monitors[0];
        long best = long.MaxValue;
        foreach (Rectangle area in Monitors)
        {
            long dx = Math.Max(area.Left - point.X, Math.Max(0, point.X - area.Right));
            long dy = Math.Max(area.Top - point.Y, Math.Max(0, point.Y - area.Bottom));
            long distance = dx * dx + dy * dy;
            if (distance < best) { nearest = area; best = distance; }
        }
        return nearest;
    }
}
