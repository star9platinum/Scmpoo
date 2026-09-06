using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using Scmpoo.Modern.Animation;
using Scmpoo.Modern.Platform;
using Scmpoo.Modern.Settings;
using Scmpoo.Modern.Tests;

namespace Scmpoo.Modern;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        string output = Path.GetFullPath(Value(args, "--output") ?? Path.Combine("artifacts", "modern-tests"));
        bool selfTest = Array.IndexOf(args, "--self-test") >= 0;
        bool uiTest = Array.IndexOf(args, "--ui-test") >= 0;
        bool stress = Array.IndexOf(args, "--stress-test") >= 0;
        try
        {
            if (selfTest)
            {
                Directory.CreateDirectory(output);
                AnimationSelfTests.Run();
                SpriteAtlasTests.Run(Assembly.GetExecutingAssembly(), output);
                AppSettings settings = new() { QuietHoursEnabled = true, QuietStartHour = 22, QuietEndHour = 8, OwnerName = "配置验证" };
                if (!settings.IsQuiet(new DateTime(2026, 9, 7, 23, 0, 0)) || settings.IsQuiet(new DateTime(2026, 9, 7, 12, 0, 0)))
                    throw new Exception("Quiet hours regression.");
                string preset = Path.Combine(output, "preset.xml");
                SettingsStore.Save(preset, settings);
                if (SettingsStore.Load(preset).OwnerName != settings.OwnerName) throw new Exception("Preset round trip regression.");
                File.WriteAllText(Path.Combine(output, "self-test.txt"), "PASS: full animation, flock, topology, sprite pixel parity, regions, fade, settings and quiet hours.\r\nCLR " + Environment.Version);
                return 0;
            }
#if MODERN_WINDOWS
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
#endif
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            if (uiTest)
            {
                if (Native.FindWindow(null, FlockContext.ControllerTitle) != IntPtr.Zero)
                    throw new InvalidOperationException("Close the running modern flock before the UI test.");
                Directory.CreateDirectory(output);
                FlockUiTests.Run(output);
                File.WriteAllText(Path.Combine(output, "ui-test.txt"), "PASS: real settings buttons, live flock application and window rendering.");
                return 0;
            }
            int count = int.TryParse(Value(args, "--count"), out int requested) ? Math.Max(1, Math.Min(32, requested)) : 0;
            bool settingsRequested = Array.IndexOf(args, "--settings") >= 0;
            using Mutex instance = new(false, "Scmpoo.Modern.Flock.v1");
            bool owned;
            try { owned = instance.WaitOne(0, false); }
            catch (AbandonedMutexException) { owned = true; }
            if (!owned)
            {
                if (stress) throw new InvalidOperationException("Close the running modern flock before the stress test.");
                Stopwatch wait = Stopwatch.StartNew();
                do
                {
                    IntPtr controller = Native.FindWindow(null, FlockContext.ControllerTitle);
                    if (controller != IntPtr.Zero)
                    {
                        Native.PostMessage(controller, FlockContext.ControllerMessage, new IntPtr(Math.Max(1, count)), settingsRequested ? new IntPtr(1) : IntPtr.Zero);
                        return 0;
                    }
                    Thread.Sleep(50);
                } while (wait.ElapsedMilliseconds < 3000);
                throw new InvalidOperationException("The existing flock did not finish starting.");
            }
            try
            {
                AppSettings settings;
                try { settings = stress ? new AppSettings { AlwaysMoving = true, Speech = false } : SettingsStore.Load(SettingsStore.DefaultPath); }
                catch (Exception error)
                {
                    MessageBox.Show("无法读取设置，将使用默认值。\n" + error.Message, "Screen Mate", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    settings = new AppSettings();
                }
                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);
                using FlockContext flock = new(settings, stress ? 32 : count == 0 ? settings.Count : count, settingsRequested, stress ? output : null);
                Application.Run(flock);
            }
            finally { instance.ReleaseMutex(); }
            return 0;
        }
        catch (Exception error)
        {
            try
            {
                Directory.CreateDirectory(output);
                File.WriteAllText(Path.Combine(output, "error.txt"), error.ToString());
            }
            catch (Exception loggingError) { Debug.WriteLine(loggingError); }
            if (!selfTest && !uiTest && !stress) MessageBox.Show(error.Message, "Screen Mate", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return 1;
        }
    }

    private static string? Value(string[] args, string key)
    {
        int index = Array.IndexOf(args, key);
        return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
    }
}
