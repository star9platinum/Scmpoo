using System;
using System.Collections.Generic;
using System.Drawing;

namespace Scmpoo.Modern.Animation;

// Uses the real machine with a deterministic desktop, clock, pointer and
// sound sink. No HWND, audio device or display server is required.
public static class AnimationSelfTests
{
    public static string Run()
    {
        VerifyBaaCompletion(false);
        VerifyBaaCompletion(true);
        VerifyChimeCancellation();
        VerifyMonitorRemoval();
        VerifyZeroCoordinateLanding();
        VerifyZeroCoordinateWindowEdge();
        VerifySleepAndWake();
        VerifyLiveOptions();
        int ticks = ExerciseAllActions();
        ticks += ExerciseFlock();
        return "Animation checks passed: 32-actor baa (sound on/off), alarm cancellation, " +
            "negative monitor removal, zero-coordinate landing/window edge, sleep/wake, all " +
            Enum.GetValues(typeof(SheepAction)).Length + " actions; " + ticks + " simulation steps.";
    }

    private static void VerifyBaaCompletion(bool sound)
    {
        var desktop = new TestEnvironment();
        for (int i = 0; i < 32; i++)
        {
            var sheep = Create(desktop, i, false);
            sheep.Options.Sound = sound;
            sheep.StartAction(SheepAction.Baa);
        }
        for (int tick = 0; tick < 8; tick++) Advance(desktop);
        foreach (SheepActor sheep in desktop.Sheep)
            Require(sheep.State == 1, "Baa did not finish after its eight frames.");
        Require(desktop.SoundRequests == (sound ? 32 : 0), "Baa sound requests were duplicated or ignored.");
    }

    private static void VerifyChimeCancellation()
    {
        var desktop = new TestEnvironment { Now = new DateTime(2026, 9, 7, 12, 0, 0) };
        SheepActor sheep = Create(desktop, 4, false);
        sheep.Options.Chime = true;
        Advance(desktop);
        Require(sheep.State == 82, "The hourly chime did not start.");
        sheep.Options.Chime = false;
        Advance(desktop);
        Require(sheep.State != 81 && sheep.State != 82, "Disabling the alarm stranded its animation.");
    }

    private static void VerifyMonitorRemoval()
    {
        var desktop = new TestEnvironment();
        desktop.Monitors.Clear();
        desktop.Monitors.Add(new Rectangle(-1280, -720, 1280, 680));
        desktop.Monitors.Add(new Rectangle(0, 0, 1280, 680));
        SheepActor sheep = Create(desktop, 5, true);
        sheep.MoveTo(-600, -600);
        desktop.Monitors.RemoveAt(0);
        sheep.RecoverToVisibleMonitor();
        Require(desktop.Monitors[0].IntersectsWith(new Rectangle(sheep.X, sheep.Y, 40, 40)),
            "Removing the negative-coordinate monitor left the actor invisible.");
    }

    private static void VerifyZeroCoordinateLanding()
    {
        var desktop = new TestEnvironment();
        desktop.Monitors.Clear();
        desktop.Monitors.Add(new Rectangle(-1280, -720, 1280, 720));
        SheepActor sheep = Create(desktop, 6, true);
        sheep.MoveTo(-600, -150);
        sheep.StartAction(SheepAction.Fall);
        bool reachedFloor = false;
        for (int tick = 0; tick < 160; tick++)
        {
            Advance(desktop);
            if (sheep.Y == -40) reachedFloor = true;
        }
        Require(reachedFloor, "A work area ending at Y=0 was treated as no collision.");
    }

    private static int ExerciseAllActions()
    {
        int ticks = 0;
        foreach (SheepAction action in Enum.GetValues(typeof(SheepAction)))
        {
            foreach (bool gravity in new[] { false, true })
            {
                var desktop = new TestEnvironment();
                desktop.Monitors.Clear();
                desktop.Monitors.Add(new Rectangle(-1600, -900, 1600, 860));
                SheepActor sheep = Create(desktop, 100 + (int)action, gravity);
                sheep.StartAction(action);
                bool changed = false;
                int originalFrame = sheep.MainFrame.SpriteIndex;
                for (int tick = 0; tick < 900; tick++)
                {
                    Advance(desktop);
                    VerifyFrame(sheep.MainFrame);
                    VerifyFrame(sheep.CompanionFrame);
                    changed |= sheep.MainFrame.SpriteIndex != originalFrame;
                    ticks++;
                }
                Require(changed, "The action did not animate: " + action);
            }
        }
        return ticks;
    }

    private static void VerifyZeroCoordinateWindowEdge()
    {
        int collisions = 0;
        for (int seed = 0; seed < 32; seed++)
        {
            var desktop = new TestEnvironment();
            desktop.Monitors.Clear();
            desktop.Monitors.Add(new Rectangle(-1280, -720, 2560, 1400));
            desktop.Windows.Add(new DesktopWindow(new IntPtr(1), new Rectangle(-400, -300, 400, 600)));
            SheepActor sheep = Create(desktop, seed, false);
            sheep.MoveTo(8, 0);
            sheep.StartAction(SheepAction.Run);
            for (int tick = 0; tick < 3; tick++) Advance(desktop);
            if (sheep.State == 30 && sheep.X == 0) collisions++;
        }
        Require(collisions > 0, "A window ending at X=0 was treated as no collision.");
    }

    private static void VerifySleepAndWake()
    {
        var desktop = new TestEnvironment();
        SheepActor sheep = Create(desktop, 10, false);
        sheep.Options.AlwaysMoving = false;
        bool slept = false;
        for (int tick = 0; tick < 1200; tick++)
        {
            Advance(desktop);
            if (sheep.IsSleeping) { slept = true; break; }
        }
        Require(slept, "An idle actor never entered its original sleep sequence.");
        desktop.PointerOverride = new Point(500, 200);
        Advance(desktop);
        Require(!sheep.IsSleeping, "Pointer activity did not wake the actor.");
    }

    private static int ExerciseFlock()
    {
        var desktop = new TestEnvironment();
        desktop.Monitors.Clear();
        desktop.Monitors.Add(new Rectangle(-1600, -900, 1600, 860));
        desktop.Monitors.Add(new Rectangle(0, 0, 1920, 1040));
        var random = new Random(918);
        var actions = (SheepAction[])Enum.GetValues(typeof(SheepAction));
        for (int i = 0; i < 32; i++)
        {
            SheepActor sheep = Create(desktop, 1800 + i, i % 2 == 0);
            sheep.Options.SpecialFrequencyPercent = 500;
            sheep.MoveTo(-1500 + (i % 8) * 130, -700 + (i / 8) * 140);
        }
        for (int tick = 0; tick < 6000; tick++)
        {
            if (tick % 120 == 0)
            {
                SheepActor sheep = desktop.Sheep[random.Next(desktop.Sheep.Count)];
                sheep.StartAction(actions[random.Next(actions.Length)]);
            }
            Advance(desktop);
            foreach (SheepActor sheep in desktop.Sheep)
            {
                VerifyFrame(sheep.MainFrame);
                VerifyFrame(sheep.CompanionFrame);
            }
        }
        return 6000 * 32;
    }

    private static void VerifyLiveOptions()
    {
        var desktop = new TestEnvironment();
        SheepActor sheep = Create(desktop, 14, false);
        sheep.StartAction(SheepAction.Walk);
        sheep.ApplyOptions(new AnimationOptions { Gravity = true, AlwaysMoving = true });
        Require(sheep.State == 96, "Enabling gravity was deferred until the old action ended.");
        sheep.ApplyOptions(new AnimationOptions { Gravity = false, AlwaysMoving = true });
        Require(sheep.State == 1, "Disabling gravity left the actor in its falling state.");
        sheep.StartAction(SheepAction.Ufo);
        sheep.ApplyOptions(new AnimationOptions { Gravity = true, AlwaysMoving = true });
        Require(sheep.State == (int)SheepAction.Ufo, "Applying options interrupted a complete special scene.");
    }

    private static SheepActor Create(TestEnvironment desktop, int seed, bool gravity)
    {
        var sheep = new SheepActor(desktop, seed, new AnimationOptions
        {
            Sound = true, Gravity = gravity, AlwaysMoving = true, SpecialFrequencyPercent = 0
        });
        desktop.Sheep.Add(sheep);
        return sheep;
    }

    private static void VerifyFrame(SpriteFrame frame)
    {
        if (!frame.Visible) return;
        int index = frame.SpriteIndex >= 256 ? frame.SpriteIndex - 256 : frame.SpriteIndex;
        Require(index >= 0 && index < 176, "Invalid sprite index: " + frame.SpriteIndex);
        Require(frame.BeamHeight >= 0 && frame.BeamHeight < 10000, "Invalid UFO beam size.");
        Require(Math.Abs(frame.X) < 100000 && Math.Abs(frame.Y) < 100000, "Animation position escaped desktop bounds.");
    }

    private static void Advance(TestEnvironment desktop)
    {
        desktop.TimestampMilliseconds += 108;
        desktop.Now = desktop.Now.AddMilliseconds(108);
        foreach (SheepActor sheep in desktop.Sheep) sheep.Tick();
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private sealed class TestEnvironment : IAnimationEnvironment
    {
        public readonly List<Rectangle> Monitors = new() { new Rectangle(0, 0, 1280, 680) };
        public Rectangle DesktopBounds
        {
            get
            {
                Rectangle result = Monitors[0];
                for (int i = 1; i < Monitors.Count; i++) result = Rectangle.Union(result, Monitors[i]);
                return result;
            }
        }
        public IList<Rectangle> MonitorWorkAreas => Monitors;
        public IList<DesktopWindow> Windows { get; } = new List<DesktopWindow>();
        public IList<SheepActor> Sheep { get; } = new List<SheepActor>();
        public Point? PointerOverride { get; set; }
        public Point PointerPosition => PointerOverride ?? new Point(DesktopBounds.Left + 200, DesktopBounds.Top + 200);
        public DateTime Now { get; set; } = new(2026, 9, 7, 12, 15, 0);
        public long TimestampMilliseconds { get; set; }
        public int SoundRequests { get; private set; }
        public IntPtr ForegroundWindow => IntPtr.Zero;
        public bool IsWindow(IntPtr window) => window != IntPtr.Zero;
        public bool IsSheepWindow(IntPtr window) => false;
        public Rectangle GetWindowBounds(IntPtr window)
        {
            foreach (DesktopWindow desktopWindow in Windows)
                if (desktopWindow.Handle == window) return desktopWindow.Bounds;
            return Rectangle.Empty;
        }
        public Rectangle GetWorkArea(Point point)
        {
            Rectangle result = Monitors[0];
            long best = long.MaxValue;
            foreach (Rectangle monitor in Monitors)
            {
                long dx = Math.Max(monitor.Left - point.X, Math.Max(0, point.X - monitor.Right));
                long dy = Math.Max(monitor.Top - point.Y, Math.Max(0, point.Y - monitor.Bottom));
                long distance = dx * dx + dy * dy;
                if (distance < best) { best = distance; result = monitor; }
            }
            return result;
        }
        public IntPtr WindowAt(Point point) => IntPtr.Zero;
        public void BringToFront(SheepActor sheep, bool companion) { }
        public void PlaceBehind(SheepActor sheep, bool companion, IntPtr window) { }
        public void PlaySound(SheepActor sheep, int resourceId, bool loop) => SoundRequests++;
        public void StopSound(SheepActor sheep) { }
    }
}

#if ANIMATION_TEST_ENTRY
internal static class AnimationTestEntry
{
    private static int Main()
    {
        try
        {
            Console.WriteLine(AnimationSelfTests.Run());
            Console.WriteLine("Runtime: " + Environment.Version);
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine(error);
            return 1;
        }
    }
}
#endif
