using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using Scmpoo.Modern.Settings;
using Scmpoo.Modern.UI;

namespace Scmpoo.Modern.Tests;

public static class FlockUiTests
{
    public static void Run(string outputDirectory)
    {
        Directory.CreateDirectory(outputDirectory);
        string settingsPath = SettingsStore.DefaultPath;
        byte[]? previousSettings = File.Exists(settingsPath) ? File.ReadAllBytes(settingsPath) : null;
        FlockContext? context = null;
        try
        {
            context = new FlockContext(new AppSettings
            {
                Count = 3, Paused = true, Sound = false, Speech = false,
                AlwaysMoving = true, Gravity = false
            }, 3, false, null);
            List<FlockContext.Entry> entries = Field<List<FlockContext.Entry>>(context, "entries");
            Assert(entries.Count == 3, "The initial flock did not contain three sheep.");
            FlockContext.Entry first = entries[0];
            FlockContext.Entry second = entries[1];
            SettingsForm firstForm = OpenSettings(context, first);
            SettingsForm secondForm = OpenSettings(context, second);
            Field<TextBox>(secondForm, "ownerName").Text = "Pending before explicit global apply";
            Field<TextBox>(firstForm, "ownerName").Text = "立即同步验证";
            Field<NumericUpDown>(firstForm, "speed").Value = 175;
            Field<NumericUpDown>(firstForm, "frequency").Value = 350;
            Field<NumericUpDown>(firstForm, "scale").Value = 2;
            Field<NumericUpDown>(firstForm, "count").Value = 32;
            FindButton(firstForm, "立即应用到所有小羊").PerformClick();
            Assert(entries.Count == 32, "The global apply button did not immediately create 32 sheep.");
            foreach (FlockContext.Entry entry in entries)
            {
                Assert(entry.Settings.OwnerName == "立即同步验证" && entry.Settings.SpeedPercent == 175 &&
                    entry.Settings.SpecialFrequencyPercent == 350 && entry.Settings.Scale == 2 && entry.Settings.Count == 32,
                    "Global settings did not immediately reach every sheep.");
            }
            Assert(Field<TextBox>(secondForm, "ownerName").Text == "立即同步验证" &&
                Field<NumericUpDown>(secondForm, "speed").Value == 175 &&
                Field<NumericUpDown>(secondForm, "frequency").Value == 350 &&
                Field<NumericUpDown>(secondForm, "count").Value == 32,
                "An already-open settings window did not immediately refresh after global apply.");
            Assert(SettingsStore.Load(settingsPath).OwnerName == "立即同步验证", "Global apply was not persisted.");
            Pump();
            SaveSettingsScreenshot(firstForm, secondForm, outputDirectory);
            SaveSpriteScreenshot(entries, outputDirectory);

            Field<TextBox>(secondForm, "ownerName").Text = "另一窗口尚未提交";
            Field<NumericUpDown>(secondForm, "speed").Value = 135;
            Field<NumericUpDown>(secondForm, "frequency").Value = 275;
            Field<TextBox>(firstForm, "ownerName").Text = "只修改当前小羊";
            Field<NumericUpDown>(firstForm, "speed").Value = 80;
            Field<NumericUpDown>(firstForm, "frequency").Value = 125;
            FindButton(firstForm, "应用到当前小羊").PerformClick();
            Assert(first.Settings.OwnerName == "只修改当前小羊" && first.Settings.SpeedPercent == 80, "Current-only apply missed its source.");
            foreach (FlockContext.Entry entry in entries)
            {
                if (ReferenceEquals(entry, first)) continue;
                Assert(entry.Settings.OwnerName == "立即同步验证" && entry.Settings.SpeedPercent == 175 &&
                    entry.Settings.SpecialFrequencyPercent == 350, "Current-only apply changed another sheep.");
            }
            Assert(Field<TextBox>(secondForm, "ownerName").Text == "另一窗口尚未提交" &&
                Field<NumericUpDown>(secondForm, "speed").Value == 135 &&
                Field<NumericUpDown>(secondForm, "frequency").Value == 275,
                "Current-only apply discarded another window's pending edits.");

            FlockContext.Entry keeper = entries[entries.Count - 1];
            SettingsForm keeperForm = OpenSettings(context, keeper);
            Field<NumericUpDown>(keeperForm, "count").Value = 2;
            FindButton(keeperForm, "应用到当前小羊").PerformClick();
            Assert(entries.Count == 2 && entries.Contains(keeper) && !keeper.Main.IsDisposed && !keeperForm.IsDisposed,
                "Reducing the flock removed the sheep whose settings were being edited.");
            ToolStripMenuItem pauseMenu = Field<ToolStripMenuItem>(context, "pauseMenu");
            Field<CheckBox>(keeperForm, "paused").Checked = false;
            FindButton(keeperForm, "立即应用到所有小羊").PerformClick();
            Assert(entries.TrueForAll(entry => !entry.Settings.Paused) && pauseMenu.CheckState == CheckState.Unchecked,
                "The tray pause state did not reflect global resume.");
            Field<CheckBox>(keeperForm, "paused").Checked = true;
            FindButton(keeperForm, "立即应用到所有小羊").PerformClick();
            Assert(entries.TrueForAll(entry => entry.Settings.Paused) && pauseMenu.CheckState == CheckState.Checked,
                "The tray pause state did not reflect global pause.");
            Field<CheckBox>(keeperForm, "paused").Checked = false;
            FindButton(keeperForm, "应用到当前小羊").PerformClick();
            Assert(pauseMenu.CheckState == CheckState.Indeterminate, "Mixed pause settings were not shown in the tray.");
            pauseMenu.PerformClick();
            Assert(pauseMenu.CheckState != CheckState.Indeterminate &&
                entries.TrueForAll(entry => entry.Settings.Paused == pauseMenu.Checked),
                "The actual tray pause command did not update the whole flock.");
            Console.WriteLine("Flock UI: actual apply-all button, 3-to-32 count, open dialog refresh, current-only isolation, pending edits, source retention, tray pause and sprite shapes passed.");
        }
        finally
        {
            try
            {
                if (context != null) { context.ExitThread(); context.Dispose(); }
            }
            finally
            {
                if (previousSettings != null) File.WriteAllBytes(settingsPath, previousSettings);
                else if (File.Exists(settingsPath)) File.Delete(settingsPath);
            }
        }
    }

    private static SettingsForm OpenSettings(FlockContext context, FlockContext.Entry entry)
    {
        MethodInfo method = typeof(FlockContext).GetMethod("OpenSettings", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Settings entry point was not found.");
        method.Invoke(context, new object[] { entry });
        Pump();
        return entry.SettingsWindow ?? throw new InvalidOperationException("Settings window did not open.");
    }

    private static T Field<T>(object instance, string name)
    {
        FieldInfo field = instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
            ?? throw new InvalidOperationException("Test field was not found: " + name);
        return (T)(field.GetValue(instance) ?? throw new InvalidOperationException("Test field was null: " + name));
    }

    private static Button FindButton(Control control, string text)
    {
        Button? button = FindControl<Button>(control, text);
        return button ?? throw new InvalidOperationException("Settings button was not found: " + text);
    }

    private static T? FindControl<T>(Control parent, string? text = null) where T : Control
    {
        if (parent is T match && (text == null || match.Text == text)) return match;
        foreach (Control child in parent.Controls)
        {
            T? found = FindControl<T>(child, text);
            if (found != null) return found;
        }
        return null;
    }

    private static void SaveSettingsScreenshot(SettingsForm first, SettingsForm second, string directory)
    {
        TabControl tabs = FindControl<TabControl>(second) ?? throw new InvalidOperationException("Settings tabs were not found.");
        tabs.SelectedIndex = 1;
        Pump();
        using Bitmap left = new(first.Width, first.Height);
        using Bitmap right = new(second.Width, second.Height);
        first.DrawToBitmap(left, new Rectangle(Point.Empty, left.Size));
        second.DrawToBitmap(right, new Rectangle(Point.Empty, right.Size));
        using Bitmap combined = new(left.Width + right.Width + 12, Math.Max(left.Height, right.Height));
        using (Graphics graphics = Graphics.FromImage(combined))
        {
            graphics.Clear(Color.FromArgb(225, 230, 234));
            graphics.DrawImageUnscaled(left, 0, 0);
            graphics.DrawImageUnscaled(right, left.Width + 12, 0);
        }
        combined.Save(Path.Combine(directory, "settings-apply-all.png"), ImageFormat.Png);
    }

    private static void SaveSpriteScreenshot(List<FlockContext.Entry> entries, string directory)
    {
        using Bitmap combined = new(6 * 100, 100, PixelFormat.Format32bppArgb);
        using Graphics graphics = Graphics.FromImage(combined);
        graphics.Clear(Color.FromArgb(225, 230, 234));
        for (int index = 0; index < 6; index++)
        {
            SpriteWindow window = entries[index].Main;
            Assert(window.Visible && window.Width == 80 && window.Height == 80, "Global sprite scaling was not rendered immediately.");
            using Bitmap frame = new(window.Width, window.Height, PixelFormat.Format32bppArgb);
            window.DrawToBitmap(frame, new Rectangle(Point.Empty, frame.Size));
            using Region region = window.Region?.Clone() ?? throw new InvalidOperationException("A sprite window had no transparency region.");
            int visiblePixels = 0;
            bool hasColor = false;
            for (int y = 0; y < frame.Height; y++)
            {
                for (int x = 0; x < frame.Width; x++)
                {
                    if (!region.IsVisible(x, y)) frame.SetPixel(x, y, Color.Transparent);
                    else
                    {
                        visiblePixels++;
                        Color color = frame.GetPixel(x, y);
                        if (color.R > 50 || color.G > 50 || color.B > 50) hasColor = true;
                    }
                }
            }
            Assert(visiblePixels > 100 && visiblePixels < frame.Width * frame.Height && hasColor, "A rendered sprite was blank or lacked a silhouette.");
            graphics.DrawImageUnscaled(frame, index * 100 + 10, 10);
        }
        combined.Save(Path.Combine(directory, "flock-rendered-shapes.png"), ImageFormat.Png);
    }

    private static void Pump()
    {
        Application.DoEvents();
        Thread.Sleep(20);
        Application.DoEvents();
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
