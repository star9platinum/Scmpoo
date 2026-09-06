using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using System.Xml;
using Microsoft.Win32;
using Scmpoo.Modern.Animation;
using Scmpoo.Modern.Platform;
using Scmpoo.Modern.Rendering;
using Scmpoo.Modern.Services;
using Scmpoo.Modern.Settings;
using Scmpoo.Modern.UI;

namespace Scmpoo.Modern;

internal sealed class FlockContext : ApplicationContext
{
    internal const string ControllerTitle = "Scmpoo.Modern.Flock.Controller.v1";
    internal const uint ControllerMessage = 0x8000 + 22;
    private readonly List<Entry> entries = new();
    private readonly List<SheepActor> actors = new();
    private readonly SpriteAtlas atlas = new();
    private readonly SoundService sound = new();
    private readonly DesktopSnapshot desktop = new();
    private readonly Stopwatch elapsed = Stopwatch.StartNew();
    private readonly Timer timer = new() { Interval = 27 };
    private readonly NotifyIcon tray;
    private readonly ToolStripMenuItem pauseMenu = new("暂停全部") { CheckOnClick = true };
    private readonly Controller controller;
    private readonly Random random = new();
    private readonly string? stressOutput;
    private readonly Process process = Process.GetCurrentProcess();
    private AppSettings defaults;
    private SheepActor? soundOwner;
    private long lastReminder;
    private long lastService;
    private long ticks;
    private long simulationSteps;
#if !LEGACY_WINDOWS
    private double cpuStart;
#endif
    private int stressStage;
    private bool shuttingDown;
    private volatile bool topologyChanged;
    private bool updatingPauseMenu;

    internal FlockContext(AppSettings settings, int count, bool openSettings, string? stressDirectory)
    {
        defaults = settings.Clone();
        stressOutput = stressDirectory;
        desktop.Refresh(0, true);
        controller = new Controller(this);
        _ = controller.Handle;
        using Stream iconStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Scmpoo.Assets.100.ico")!;
        tray = new NotifyIcon { Icon = new Icon(iconStream), Text = "Screen Mate 3.0", Visible = true };
        ContextMenuStrip menu = new();
        menu.Items.Add("小羊设置", null, (_, _) => OpenSettings(entries[0]));
        menu.Items.Add("添加小羊", null, (_, _) => SetCount(entries.Count + 1));
        menu.Items.Add("开启 32 只小羊", null, (_, _) => SetCount(32));
        pauseMenu.Checked = defaults.Paused;
        pauseMenu.CheckedChanged += (_, _) =>
        {
            if (updatingPauseMenu) return;
            foreach (Entry entry in entries) entry.Settings.Paused = pauseMenu.Checked;
            defaults.Paused = pauseMenu.Checked;
            if (pauseMenu.Checked) { sound.Stop(); soundOwner = null; }
            RefreshPauseWindows();
        };
        menu.Items.Add(pauseMenu);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("关闭所有小羊", null, (_, _) => ExitThread());
        tray.ContextMenuStrip = menu;
        tray.DoubleClick += (_, _) => OpenSettings(entries[0]);
        SetCount(count);
        SystemEvents.DisplaySettingsChanged += OnDisplayChanged;
        SystemEvents.UserPreferenceChanged += OnPreferencesChanged;
        timer.Tick += Tick;
#if !LEGACY_WINDOWS
        cpuStart = process.TotalProcessorTime.TotalMilliseconds;
#endif
        timer.Start();
        if (openSettings) OpenSettings(entries[0]);
    }

    private void SetCount(int count)
    {
        count = Math.Max(1, Math.Min(32, count));
        while (entries.Count < count)
        {
            AppSettings settings = defaults.Clone();
            ActorEnvironment environment = new(this, settings);
            SheepActor actor = new(environment, random.Next(), settings.AnimationOptions());
            Entry entry = new(actor, settings, environment);
            entries.Add(entry);
            actors.Add(actor);
            entry.Main = MakeWindow(entry, false);
            actor.MainWindowHandle = entry.Main.Handle;
            Rectangle area = environment.MonitorWorkAreas[random.Next(environment.MonitorWorkAreas.Count)];
            actor.MoveTo(area.Left + random.Next(Math.Max(1, area.Width - 40)),
                area.Top + random.Next(Math.Max(1, area.Height - 40)));
            entry.NextStep = elapsed.ElapsedMilliseconds + entries.Count * 108 / 32;
            Render(entry);
        }
        while (entries.Count > count) Remove(entries[entries.Count - 1]);
        defaults.Count = entries.Count;
        foreach (Entry entry in entries) entry.Settings.Count = entries.Count;
        RefreshCounts();
    }

    private SpriteWindow MakeWindow(Entry entry, bool companion) => new(atlas, companion,
        point => { entry.Actor.DragTo(point.X, point.Y); Render(entry); },
        entry.Actor.EndDrag, () => OpenSettings(entry),
        action => { entry.Actor.StartAction(action); entry.NextStep = 0; },
        () => { Remove(entry); if (entries.Count == 0) ExitThread(); },
        () => SetCount(32), () => ExitThread(), path =>
        {
            entry.Actor.StartAction(SheepAction.Flower);
            ShowSpeech(entry, string.IsNullOrEmpty(entry.Settings.OwnerName)
                ? "谢谢，好吃！" : entry.Settings.OwnerName + "，谢谢，好吃！");
            if (entry.Settings.Sound && !entry.Settings.Paused && !entry.Settings.IsQuiet(DateTime.Now) &&
                string.Equals(Path.GetExtension(path), ".wav", StringComparison.OrdinalIgnoreCase))
            {
                if (sound.TryPlayFile(path)) soundOwner = entry.Actor;
            }
        });

    private void Remove(Entry entry)
    {
        if (ReferenceEquals(soundOwner, entry.Actor)) { sound.Stop(); soundOwner = null; }
        CloseSpeech(entry);
        entry.SettingsWindow?.Close();
        entry.Companion?.Dispose();
        entry.Main.Dispose();
        entries.Remove(entry);
        actors.Remove(entry.Actor);
        if (!shuttingDown && entries.Count > 0)
        {
            defaults.Count = entries.Count;
            foreach (Entry remaining in entries) remaining.Settings.Count = entries.Count;
            RefreshCounts();
        }
    }

    private void Tick(object? sender, EventArgs args)
    {
        long now = elapsed.ElapsedMilliseconds;
        ticks++;
        desktop.Refresh(now);
        if (topologyChanged)
        {
            topologyChanged = false;
            desktop.Refresh(now, true);
            foreach (Entry entry in entries)
            {
                entry.Environment.UpdateAreas();
                entry.Actor.RecoverToVisibleMonitor();
                Render(entry);
            }
        }
        bool allPaused = true;
        foreach (Entry entry in entries)
        {
            if (entry.Settings.Paused) { entry.NextStep = now; continue; }
            allPaused = false;
            if (now < entry.NextStep) continue;
            int period = 10800 / entry.Settings.SpeedPercent;
            entry.NextStep = Math.Max(entry.NextStep + period, now + 1);
            entry.Actor.Tick();
            simulationSteps++;
            if (entry.Settings.FollowPointer && !entry.Actor.IsDragging && !entry.Actor.IsSpecialAction)
            {
                Rectangle area = entry.Environment.GetWorkArea(desktop.Pointer);
                int x = Math.Max(area.Left, Math.Min(desktop.Pointer.X - 20, area.Right - 40));
                int y = Math.Max(area.Top, Math.Min(desktop.Pointer.Y - 40, area.Bottom - 40));
                entry.Actor.MoveTo(entry.Actor.X + Math.Max(-8, Math.Min(8, x - entry.Actor.X)),
                    entry.Actor.Y + Math.Max(-8, Math.Min(8, y - entry.Actor.Y)));
            }
            Render(entry);
        }
        timer.Interval = allPaused ? 250 : 27;
        if (now - lastService >= 1000)
        {
            lastService = now;
            foreach (Entry entry in entries)
            {
                if (entry.Speech != null && (!entry.Settings.Speech || now >= entry.SpeechDeadline))
                    CloseSpeech(entry);
            }
            if (soundOwner != null)
            {
                Entry? owner = entries.Find(entry => ReferenceEquals(entry.Actor, soundOwner));
                if (owner == null || !owner.Settings.Sound || owner.Settings.IsQuiet(DateTime.Now))
                { sound.Stop(); soundOwner = null; }
            }
            Entry? reminder = entries.Find(entry => entry.Settings.ReminderMinutes > 0 && entry.Settings.Speech);
            if (reminder != null && now - lastReminder >= reminder.Settings.ReminderMinutes * 60000L)
            {
                lastReminder = now;
                ShowSpeech(reminder, string.IsNullOrEmpty(reminder.Settings.OwnerName)
                    ? "休息一下、喝口水吧。" : reminder.Settings.OwnerName + "，休息一下、喝口水吧。");
            }
        }
        if (stressOutput != null) StressTick(now);
    }

    private void Render(Entry entry)
    {
        entry.Main.Present(entry.Actor.MainFrame, entry.Settings.Scale, entry.Settings.AlwaysOnTop);
        if (entry.Actor.CompanionFrame.Visible && entry.Companion == null)
        {
            entry.Companion = MakeWindow(entry, true);
            entry.Actor.CompanionWindowHandle = entry.Companion.Handle;
        }
        entry.Companion?.Present(entry.Actor.CompanionFrame, entry.Settings.Scale, entry.Settings.AlwaysOnTop);
        bool companionVisible = entry.Actor.CompanionFrame.Visible;
        if (entry.Companion != null && companionVisible &&
            (!entry.CompanionWasVisible || entry.MainWasAbove != entry.Actor.MainAboveCompanion))
        {
            SpriteWindow upper = entry.Actor.MainAboveCompanion ? entry.Main : entry.Companion;
            SpriteWindow lower = entry.Actor.MainAboveCompanion ? entry.Companion : entry.Main;
            Native.SetWindowPos(lower.Handle, upper.Handle, 0, 0, 0, 0, Native.NoActivate | Native.NoSize | Native.NoMove);
        }
        entry.CompanionWasVisible = companionVisible;
        entry.MainWasAbove = entry.Actor.MainAboveCompanion;
    }

    private void OpenSettings(Entry entry)
    {
        if (entry.SettingsWindow != null) { entry.SettingsWindow.Activate(); return; }
        Screen[] screens = Screen.AllScreens;
        string[] names = new string[screens.Length];
        for (int i = 0; i < screens.Length; i++) names[i] = screens[i].DeviceName + "  " + screens[i].Bounds.Width + " x " + screens[i].Bounds.Height;
        entry.Settings.Count = entries.Count;
        entry.SettingsWindow = new SettingsForm(entry.Settings, names,
            (settings, all) => Apply(entry, settings, all, true), SetCount,
            action => { entry.Actor.StartAction(action); entry.NextStep = 0; }, () => ExitThread());
        entry.SettingsWindow.FormClosed += (_, _) => entry.SettingsWindow = null;
        entry.SettingsWindow.Show();
    }

    internal void Apply(Entry source, AppSettings settings, bool all, bool save)
    {
        settings = settings.Clone();
        settings.Validate();
        if (save) SettingsStore.Save(SettingsStore.DefaultPath, settings);
        defaults = settings.Clone();
        if (settings.Count != entries.Count)
        {
            // Keep the sheep whose settings are open when shrinking the flock.
            entries.Remove(source);
            entries.Insert(0, source);
            SetCount(settings.Count);
        }
        foreach (Entry entry in entries)
        {
            if (!all && !ReferenceEquals(entry, source)) continue;
            bool monitorChanged = entry.Settings.MonitorIndex != settings.MonitorIndex;
            entry.Settings = settings.Clone();
            entry.Environment.Settings = entry.Settings;
            entry.Environment.UpdateAreas();
            entry.Actor.ApplyOptions(settings.AnimationOptions());
            if (!entry.Settings.Speech) CloseSpeech(entry);
            else if (entry.Speech != null) entry.Speech.TopMost = entry.Settings.AlwaysOnTop;
            if (monitorChanged) entry.Actor.RecoverToVisibleMonitor();
            entry.NextStep = elapsed.ElapsedMilliseconds;
            Render(entry);
        }
        if ((all || ReferenceEquals(soundOwner, source.Actor)) &&
            (!settings.Sound || settings.Paused || settings.IsQuiet(DateTime.Now)))
        { sound.Stop(); soundOwner = null; }
        lastReminder = elapsed.ElapsedMilliseconds;
        RefreshSettingsWindows(all ? null : source);
    }

    private void RefreshSettingsWindows(Entry? source = null)
    {
        foreach (Entry entry in entries)
            if (source == null || ReferenceEquals(source, entry))
                entry.SettingsWindow?.RefreshSettings(entry.Settings);
        RefreshPauseMenu();
    }

    private void ShowSpeech(Entry entry, string message)
    {
        if (!entry.Settings.Speech) return;
        entry.Speech ??= new SpeechWindow();
        entry.Speech.ShowMessage(message,
            entry.Environment.GetWorkArea(new Point(entry.Actor.X + 20, entry.Actor.Y + 20)),
            new Point(entry.Main.Left + entry.Main.Width / 2, entry.Main.Top), entry.Settings.AlwaysOnTop);
        entry.SpeechDeadline = elapsed.ElapsedMilliseconds + 5000;
    }

    private static void CloseSpeech(Entry entry)
    {
        entry.Speech?.Dispose();
        entry.Speech = null;
        entry.SpeechDeadline = 0;
    }

    private void RefreshCounts()
    {
        foreach (Entry entry in entries) entry.SettingsWindow?.RefreshCount(entries.Count);
        RefreshPauseMenu();
    }

    private void RefreshPauseWindows()
    {
        foreach (Entry entry in entries) entry.SettingsWindow?.RefreshPause(entry.Settings.Paused);
        RefreshPauseMenu();
    }

    private void RefreshPauseMenu()
    {
        updatingPauseMenu = true;
        pauseMenu.CheckState = entries.TrueForAll(entry => entry.Settings.Paused) ? CheckState.Checked
            : entries.Exists(entry => entry.Settings.Paused) ? CheckState.Indeterminate : CheckState.Unchecked;
        updatingPauseMenu = false;
    }

    private void OnDisplayChanged(object? sender, EventArgs args) => topologyChanged = true;
    private void OnPreferencesChanged(object? sender, UserPreferenceChangedEventArgs args) => topologyChanged = true;

    private void StressTick(long now)
    {
        if (stressStage == 0 && now >= 1000)
        {
            AppSettings settings = entries[0].Settings.Clone();
            settings.SpeedPercent = 175;
            settings.SpecialFrequencyPercent = 350;
            settings.OwnerName = "Flock regression";
            Apply(entries[0], settings, true, false);
            foreach (Entry entry in entries) entry.Actor.StartAction(SheepAction.Baa);
            stressStage++;
        }
        if (stressStage == 1 && now >= 2500)
        {
            foreach (Entry entry in entries)
            {
                if (entry.Actor.State is 45 or 46 || entry.Settings.SpeedPercent != 175)
                    throw new InvalidOperationException("Flock animation/apply regression.");
                entry.Actor.StartAction(SheepAction.Bathtub);
            }
            stressStage++;
        }
        if (now < 12000) return;
        Directory.CreateDirectory(stressOutput!);
#if !LEGACY_WINDOWS
        process.Refresh();
        double cpu = process.TotalProcessorTime.TotalMilliseconds - cpuStart;
#endif
        using (XmlWriter report = XmlWriter.Create(Path.Combine(stressOutput!, "modern-stress.xml"), new XmlWriterSettings { Indent = true }))
        {
            report.WriteStartElement("StressResult");
            report.WriteElementString("Instances", entries.Count.ToString());
            report.WriteElementString("ElapsedMilliseconds", now.ToString());
#if LEGACY_WINDOWS
            report.WriteElementString("PerformanceMetrics", "UnavailableOnWin98");
#else
            report.WriteElementString("CpuMilliseconds", cpu.ToString(System.Globalization.CultureInfo.InvariantCulture));
            report.WriteElementString("CpuPercentOneCore", (cpu / now * 100).ToString("F2", System.Globalization.CultureInfo.InvariantCulture));
            report.WriteElementString("WorkingSetBytes", process.WorkingSet64.ToString());
            report.WriteElementString("PrivateBytes", process.PrivateMemorySize64.ToString());
#endif
            report.WriteElementString("TimerTicks", ticks.ToString());
            report.WriteElementString("SimulationSteps", simulationSteps.ToString());
            report.WriteElementString("DesktopEnumerations", desktop.EnumerationCount.ToString());
            report.WriteElementString("CachedSprites", atlas.CachedFrameCount.ToString());
            report.WriteEndElement();
        }
        ExitThread();
    }

    protected override void ExitThreadCore()
    {
        if (shuttingDown) return;
        shuttingDown = true;
        timer.Stop();
        timer.Dispose();
        SystemEvents.DisplaySettingsChanged -= OnDisplayChanged;
        SystemEvents.UserPreferenceChanged -= OnPreferencesChanged;
        while (entries.Count > 0) Remove(entries[entries.Count - 1]);
        tray.Visible = false;
        tray.ContextMenuStrip?.Dispose();
        tray.Icon?.Dispose();
        tray.Dispose();
        controller.Dispose();
        sound.Dispose();
        atlas.Dispose();
        process.Dispose();
        base.ExitThreadCore();
    }

    internal sealed class Entry(SheepActor actor, AppSettings settings, ActorEnvironment environment)
    {
        public readonly SheepActor Actor = actor;
        public readonly ActorEnvironment Environment = environment;
        public AppSettings Settings = settings;
        public SpriteWindow Main = null!;
        public SpriteWindow? Companion;
        public SettingsForm? SettingsWindow;
        public SpeechWindow? Speech;
        public long SpeechDeadline;
        public long NextStep;
        public bool CompanionWasVisible;
        public bool MainWasAbove;
    }

    private sealed class Controller(FlockContext flock) : Form
    {
        protected override void SetVisibleCore(bool value) => base.SetVisibleCore(false);
        protected override CreateParams CreateParams
        {
            get { CreateParams p = base.CreateParams; p.Caption = ControllerTitle; p.ExStyle |= 0x80; return p; }
        }
        protected override void WndProc(ref Message message)
        {
            if (message.Msg == ControllerMessage)
            {
                if (message.LParam != IntPtr.Zero) flock.OpenSettings(flock.entries[0]);
                else flock.SetCount(flock.entries.Count + Math.Max(1, Math.Min(32, message.WParam.ToInt32())));
                return;
            }
            base.WndProc(ref message);
        }
    }

    internal sealed class ActorEnvironment : IAnimationEnvironment
    {
        private readonly FlockContext flock;
        private readonly List<Rectangle> areas = new();
        public AppSettings Settings;
        public ActorEnvironment(FlockContext flock, AppSettings settings)
        { this.flock = flock; Settings = settings; UpdateAreas(); }
        public void UpdateAreas()
        {
            areas.Clear();
            int index = Settings.MonitorIndex;
            if (index >= 0 && index < flock.desktop.Monitors.Count) areas.Add(flock.desktop.Monitors[index]);
            else areas.AddRange(flock.desktop.Monitors);
        }
        public Rectangle DesktopBounds => areas.Count == 1 ? areas[0] : flock.desktop.Bounds;
        public IList<Rectangle> MonitorWorkAreas => areas;
        public IList<DesktopWindow> Windows => flock.desktop.Windows;
        public IList<SheepActor> Sheep => flock.actors;
        public Point PointerPosition => flock.desktop.Pointer;
        public DateTime Now => DateTime.Now;
        public long TimestampMilliseconds => flock.elapsed.ElapsedMilliseconds;
        public IntPtr ForegroundWindow => flock.desktop.Foreground;
        public bool IsWindow(IntPtr window) => Native.IsWindow(window) && !Native.IsIconic(window) && Native.IsWindowVisible(window);
        public bool IsSheepWindow(IntPtr window) => flock.actors.Exists(actor => actor.MainWindowHandle == window || actor.CompanionWindowHandle == window && window != IntPtr.Zero);
        public Rectangle GetWindowBounds(IntPtr window) => Native.GetWindowRect(window, out Native.Rect rect) ? rect.Bounds : Rectangle.Empty;
        public Rectangle GetWorkArea(Point point) => flock.desktop.Nearest(point, Settings.MonitorIndex);
        public IntPtr WindowAt(Point point) => Native.WindowFromPoint(point);
        public void BringToFront(SheepActor sheep, bool companion)
        {
            IntPtr handle = companion ? sheep.CompanionWindowHandle : sheep.MainWindowHandle;
            if (handle != IntPtr.Zero) Native.SetWindowPos(handle, Settings.AlwaysOnTop ? new IntPtr(-1) : IntPtr.Zero, 0, 0, 0, 0, Native.NoMove | Native.NoSize | Native.NoActivate);
        }
        public void PlaceBehind(SheepActor sheep, bool companion, IntPtr window)
        {
            if (Settings.AlwaysOnTop) return;
            IntPtr handle = companion ? sheep.CompanionWindowHandle : sheep.MainWindowHandle;
            if (handle != IntPtr.Zero) Native.SetWindowPos(handle, window, 0, 0, 0, 0, Native.NoMove | Native.NoSize | Native.NoActivate);
        }
        public void PlaySound(SheepActor sheep, int resourceId, bool loop)
        {
            if (!Settings.Sound || Settings.Paused || Settings.IsQuiet(DateTime.Now)) return;
            if (flock.sound.TryPlay(resourceId, loop)) flock.soundOwner = sheep;
        }
        public void StopSound(SheepActor sheep)
        {
            if (!ReferenceEquals(flock.soundOwner, sheep)) return;
            flock.sound.Stop();
            flock.soundOwner = null;
        }
    }
}
