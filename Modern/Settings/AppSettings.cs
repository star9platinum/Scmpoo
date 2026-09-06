using System;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Scmpoo.Modern.Animation;

namespace Scmpoo.Modern.Settings;

public sealed class AppSettings
{
    public bool Sound { get; set; } = true;
    public bool Chime { get; set; }
    public bool Gravity { get; set; } = true;
    public bool AlwaysMoving { get; set; }
    public bool AlwaysOnTop { get; set; }
    public bool Speech { get; set; } = true;
    public bool FollowPointer { get; set; }
    public bool Paused { get; set; }
    public bool QuietHoursEnabled { get; set; }
    public int SpeedPercent { get; set; } = 100;
    public int SpecialFrequencyPercent { get; set; } = 100;
    public int ReminderMinutes { get; set; } = 45;
    public int Scale { get; set; } = 1;
    public int Count { get; set; } = 1;
    public int MonitorIndex { get; set; } = -1;
    public int QuietStartHour { get; set; } = 22;
    public int QuietEndHour { get; set; } = 8;
    public string OwnerName { get; set; } = "";

    public AppSettings Clone() => (AppSettings)MemberwiseClone();

    public void Validate()
    {
        SpeedPercent = Bound(SpeedPercent, 50, 200);
        SpecialFrequencyPercent = Bound(SpecialFrequencyPercent, 0, 500);
        ReminderMinutes = ReminderMinutes == 0 ? 0 : Bound(ReminderMinutes, 5, 240);
        Scale = Bound(Scale, 1, 4);
        Count = Bound(Count, 1, 32);
        MonitorIndex = Math.Max(-1, MonitorIndex);
        QuietStartHour = Bound(QuietStartHour, 0, 23);
        QuietEndHour = Bound(QuietEndHour, 0, 23);
        OwnerName = OwnerName ?? "";
        if (OwnerName.Length > 40) OwnerName = OwnerName.Substring(0, 40);
    }

    public bool IsQuiet(DateTime now)
    {
        if (!QuietHoursEnabled) return false;
        if (QuietStartHour == QuietEndHour) return true;
        return QuietStartHour < QuietEndHour
            ? now.Hour >= QuietStartHour && now.Hour < QuietEndHour
            : now.Hour >= QuietStartHour || now.Hour < QuietEndHour;
    }

    public AnimationOptions AnimationOptions() => new()
    {
        Sound = Sound, Chime = Chime, Gravity = Gravity,
        AlwaysMoving = AlwaysMoving, SpecialFrequencyPercent = SpecialFrequencyPercent
    };

    private static int Bound(int value, int low, int high) => Math.Max(low, Math.Min(high, value));
}

public static class SettingsStore
{
    public static string DefaultPath => Path.Combine(Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Scmpoo"), "modern-settings.xml");

    public static AppSettings Load(string path)
    {
        if (!File.Exists(path)) return new AppSettings();
        using FileStream file = File.OpenRead(path);
        using XmlTextReader reader = new(file) { XmlResolver = null };
#if LEGACY_WINDOWS
        reader.ProhibitDtd = true;
#else
        reader.DtdProcessing = DtdProcessing.Prohibit;
#endif
        AppSettings settings = (AppSettings)new XmlSerializer(typeof(AppSettings)).Deserialize(reader)!;
        settings.Validate();
        return settings;
    }

    public static void Save(string path, AppSettings settings)
    {
        settings.Validate();
        path = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (XmlWriter writer = XmlWriter.Create(temporary, new XmlWriterSettings
            { Indent = true, Encoding = new UTF8Encoding(false) }))
                new XmlSerializer(typeof(AppSettings)).Serialize(writer, settings);
            if (File.Exists(path))
            {
#if LEGACY_WINDOWS
                // ReplaceFile is unavailable on Win98; retain the previous
                // complete settings file until the replacement is installed.
                string backup = path + "." + Guid.NewGuid().ToString("N") + ".previous";
                File.Move(path, backup);
                try { File.Move(temporary, path); }
                catch
                {
                    if (!File.Exists(path)) File.Move(backup, path);
                    throw;
                }
                File.Delete(backup);
#else
                File.Replace(temporary, path, null);
#endif
            }
            else File.Move(temporary, path);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
}
