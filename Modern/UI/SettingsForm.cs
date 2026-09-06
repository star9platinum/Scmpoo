using System;
using System.Drawing;
using System.Windows.Forms;
using Scmpoo.Modern.Animation;
using Scmpoo.Modern.Settings;

namespace Scmpoo.Modern.UI;

public sealed class SettingsForm : Form
{
    private readonly Action<AppSettings, bool> apply;
    private readonly Action<int> setCount;
    private readonly Action<SheepAction> playAction;
    private readonly Action closeAll;
    private AppSettings current;

    private readonly CheckBox sound = Toggle("动作声音");
    private readonly CheckBox chime = Toggle("整点报时");
    private readonly CheckBox gravity = Toggle("窗口重力");
    private readonly CheckBox alwaysMoving = Toggle("持续活动");
    private readonly CheckBox alwaysOnTop = Toggle("始终置顶");
    private readonly CheckBox speech = Toggle("对话气泡");
    private readonly CheckBox followPointer = Toggle("跟随鼠标");
    private readonly CheckBox paused = Toggle("暂停动画");
    private readonly CheckBox quietHours = Toggle("定时静音");
    private readonly CheckBox reminders = Toggle("休息提醒");
    private readonly NumericUpDown speed = Number(50, 200, 10);
    private readonly NumericUpDown frequency = Number(0, 500, 25);
    private readonly NumericUpDown reminderMinutes = Number(5, 240, 5);
    private readonly NumericUpDown scale = Number(1, 4, 1);
    private readonly NumericUpDown count = Number(1, 32, 1);
    private readonly NumericUpDown quietStart = Number(0, 23, 1);
    private readonly NumericUpDown quietEnd = Number(0, 23, 1);
    private readonly TextBox ownerName = new() { MaxLength = 40, Dock = DockStyle.Fill };
    private readonly ComboBox monitor = Choice();
    private readonly ComboBox action = Choice();

    public SettingsForm(AppSettings current, string[] monitorNames,
        Action<AppSettings, bool> apply, Action<int> setCount,
        Action<SheepAction> playAction, Action closeAll)
    {
        this.current = current.Clone();
        this.apply = apply;
        this.setCount = setCount;
        this.playAction = playAction;
        this.closeAll = closeAll;

        Text = "小羊设置";
        StartPosition = FormStartPosition.CenterScreen;
        AutoScaleMode = AutoScaleMode.Font;
        Font = SystemFonts.MessageBoxFont;
        ClientSize = new Size(620, 510);
        MinimumSize = new Size(560, 490);
        ShowIcon = false;
        MaximizeBox = false;

        TableLayoutPanel layout = new()
        {
            Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2,
            Padding = new Padding(10)
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        TabControl tabs = new() { Dock = DockStyle.Fill };
        layout.Controls.Add(tabs, 0, 0);

        TableLayoutPanel behavior = Page(tabs, "行为");
        AddToggle(behavior, gravity);
        AddToggle(behavior, alwaysMoving);
        AddToggle(behavior, followPointer);
        AddToggle(behavior, paused);
        AddRow(behavior, "动画速度（%）", speed);
        AddRow(behavior, "特殊动画频率（%）", frequency);

        TableLayoutPanel appearance = Page(tabs, "外观与显示器");
        AddRow(appearance, "称呼", ownerName);
        AddRow(appearance, "像素倍率", scale);
        monitor.Items.Add("所有显示器");
        foreach (string name in monitorNames) monitor.Items.Add(name);
        AddRow(appearance, "活动显示器", monitor);
        AddToggle(appearance, alwaysOnTop);
        AddToggle(appearance, speech);

        TableLayoutPanel audio = Page(tabs, "声音与提醒");
        AddToggle(audio, sound);
        AddToggle(audio, chime);
        AddToggle(audio, quietHours);
        AddRow(audio, "静音开始（时）", quietStart);
        AddRow(audio, "静音结束（时）", quietEnd);
        AddToggle(audio, reminders);
        AddRow(audio, "提醒间隔（分钟）", reminderMinutes);
        quietHours.CheckedChanged += (_, _) => UpdateEnabledState();
        reminders.CheckedChanged += (_, _) => UpdateEnabledState();

        TableLayoutPanel flock = Page(tabs, "群体与动作");
        FlowLayoutPanel countControls = Flow();
        countControls.Controls.Add(count);
        countControls.Controls.Add(Command("调整数量", (_, _) => Execute(() =>
        {
            this.setCount((int)count.Value);
            this.current.Count = (int)count.Value;
        })));
        AddRow(flock, "小羊数量", countControls);
        foreach (SheepAction item in Enum.GetValues(typeof(SheepAction)))
            action.Items.Add(new ActionChoice(item));
        action.SelectedIndex = 0;
        AddRow(flock, "指定动作", action);
        AddRow(flock, "", Command("播放动作", (_, _) => Execute(() =>
        {
            if (action.SelectedItem is ActionChoice selected) this.playAction(selected.Value);
        })));

        FlowLayoutPanel presets = Flow();
        presets.Controls.Add(Command("导入预设...", (_, _) => ImportPreset()));
        presets.Controls.Add(Command("导出预设...", (_, _) => ExportPreset()));
        AddRow(flock, "设置预设", presets);
        AddRow(flock, "", Command("关闭所有小羊", (_, _) => Execute(() =>
        {
            this.closeAll();
            Close();
        })));

        FlowLayoutPanel footer = Flow();
        footer.Dock = DockStyle.Fill;
        footer.FlowDirection = FlowDirection.RightToLeft;
        footer.Padding = new Padding(0, 8, 0, 0);
        Button close = Command("关闭", (_, _) => Close());
        close.DialogResult = DialogResult.Cancel;
        footer.Controls.Add(close);
        footer.Controls.Add(Command("立即应用到所有小羊", (_, _) => ApplySettings(true)));
        footer.Controls.Add(Command("应用到当前小羊", (_, _) => ApplySettings(false)));
        layout.Controls.Add(footer, 0, 1);
        Controls.Add(layout);
        CancelButton = close;
        RefreshSettings(current);
    }

    public void RefreshSettings(AppSettings settings)
    {
        if (IsDisposed) return;
        if (InvokeRequired)
        {
            AppSettings snapshot = settings.Clone();
            BeginInvoke(new MethodInvoker(() => RefreshSettings(snapshot)));
            return;
        }
        current = settings.Clone();
        sound.Checked = settings.Sound;
        chime.Checked = settings.Chime;
        gravity.Checked = settings.Gravity;
        alwaysMoving.Checked = settings.AlwaysMoving;
        alwaysOnTop.Checked = settings.AlwaysOnTop;
        speech.Checked = settings.Speech;
        followPointer.Checked = settings.FollowPointer;
        paused.Checked = settings.Paused;
        quietHours.Checked = settings.QuietHoursEnabled;
        reminders.Checked = settings.ReminderMinutes > 0;
        SetNumber(speed, settings.SpeedPercent);
        SetNumber(frequency, settings.SpecialFrequencyPercent);
        SetNumber(reminderMinutes, settings.ReminderMinutes > 0 ? settings.ReminderMinutes : 45);
        SetNumber(scale, settings.Scale);
        SetNumber(count, settings.Count);
        SetNumber(quietStart, settings.QuietStartHour);
        SetNumber(quietEnd, settings.QuietEndHour);
        ownerName.Text = settings.OwnerName ?? "";
        monitor.SelectedIndex = Math.Max(0, Math.Min(monitor.Items.Count - 1, settings.MonitorIndex + 1));
        UpdateEnabledState();
    }

    public void RefreshCount(int value)
    {
        if (IsDisposed) return;
        if (InvokeRequired)
        {
            BeginInvoke(new MethodInvoker(() => RefreshCount(value)));
            return;
        }
        current.Count = Math.Max(1, Math.Min(32, value));
        SetNumber(count, current.Count);
    }

    public void RefreshPause(bool value)
    {
        if (IsDisposed) return;
        if (InvokeRequired)
        {
            BeginInvoke(new MethodInvoker(() => RefreshPause(value)));
            return;
        }
        current.Paused = value;
        paused.Checked = value;
    }

    private AppSettings ReadSettings()
    {
        AppSettings result = current.Clone();
        result.Sound = sound.Checked;
        result.Chime = chime.Checked;
        result.Gravity = gravity.Checked;
        result.AlwaysMoving = alwaysMoving.Checked;
        result.AlwaysOnTop = alwaysOnTop.Checked;
        result.Speech = speech.Checked;
        result.FollowPointer = followPointer.Checked;
        result.Paused = paused.Checked;
        result.QuietHoursEnabled = quietHours.Checked;
        result.SpeedPercent = (int)speed.Value;
        result.SpecialFrequencyPercent = (int)frequency.Value;
        result.ReminderMinutes = reminders.Checked ? (int)reminderMinutes.Value : 0;
        result.Scale = (int)scale.Value;
        result.Count = (int)count.Value;
        result.QuietStartHour = (int)quietStart.Value;
        result.QuietEndHour = (int)quietEnd.Value;
        result.OwnerName = ownerName.Text.Trim();
        result.MonitorIndex = monitor.SelectedIndex - 1;
        return result;
    }

    private void ApplySettings(bool all) => Execute(() =>
    {
        AppSettings settings = ReadSettings();
        apply(settings.Clone(), all);
        current = settings;
    });

    private void UpdateEnabledState()
    {
        quietStart.Enabled = quietHours.Checked;
        quietEnd.Enabled = quietHours.Checked;
        reminderMinutes.Enabled = reminders.Checked;
    }

    private void ImportPreset()
    {
        using OpenFileDialog dialog = new()
        {
            Filter = "小羊预设 (*.xml)|*.xml|所有文件 (*.*)|*.*",
            Title = "导入小羊预设", CheckFileExists = true, RestoreDirectory = true
        };
        if (dialog.ShowDialog(this) == DialogResult.OK)
            Execute(() => RefreshSettings(SettingsStore.Load(dialog.FileName)));
    }

    private void ExportPreset()
    {
        using SaveFileDialog dialog = new()
        {
            Filter = "小羊预设 (*.xml)|*.xml", DefaultExt = "xml", AddExtension = true,
            FileName = "scmpoo-preset.xml", Title = "导出小羊预设",
            OverwritePrompt = true, RestoreDirectory = true
        };
        if (dialog.ShowDialog(this) == DialogResult.OK)
            Execute(() => SettingsStore.Save(dialog.FileName, ReadSettings()));
    }

    private void Execute(Action command)
    {
        try { command(); }
        catch (Exception error)
        {
            MessageBox.Show(this, error.Message, "操作未完成", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private static CheckBox Toggle(string text) => new()
    {
        Text = text, AutoSize = true, Anchor = AnchorStyles.Left,
        Margin = new Padding(0, 7, 0, 7)
    };

    private static NumericUpDown Number(int minimum, int maximum, int increment) => new()
    {
        Minimum = minimum, Maximum = maximum, Increment = increment,
        Width = 105, Anchor = AnchorStyles.Left, Margin = new Padding(0, 4, 6, 4)
    };

    private static ComboBox Choice() => new()
    {
        DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill,
        Margin = new Padding(0, 4, 0, 4), IntegralHeight = true
    };

    private static Button Command(string text, EventHandler click)
    {
        Button button = new()
        {
            Text = text, AutoSize = true, MinimumSize = new Size(84, 30),
            Margin = new Padding(0, 3, 6, 3), Padding = new Padding(7, 0, 7, 0)
        };
        button.Click += click;
        return button;
    }

    private static FlowLayoutPanel Flow() => new()
    {
        AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
        Margin = Padding.Empty, WrapContents = true
    };

    private static TableLayoutPanel Page(TabControl tabs, string title)
    {
        TabPage page = new(title) { AutoScroll = true, UseVisualStyleBackColor = true };
        TableLayoutPanel table = new()
        {
            AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Dock = DockStyle.Top, ColumnCount = 2, Padding = new Padding(14),
            GrowStyle = TableLayoutPanelGrowStyle.AddRows
        };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 43));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 57));
        page.Controls.Add(table);
        tabs.TabPages.Add(page);
        return table;
    }

    private static void AddToggle(TableLayoutPanel table, CheckBox control)
    {
        int row = table.RowCount++;
        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        table.Controls.Add(control, 0, row);
        table.SetColumnSpan(control, 2);
    }

    private static void AddRow(TableLayoutPanel table, string text, Control control)
    {
        int row = table.RowCount++;
        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        table.Controls.Add(new Label
        {
            Text = text, AutoSize = true, Anchor = AnchorStyles.Left,
            Margin = new Padding(0, 8, 10, 8)
        }, 0, row);
        table.Controls.Add(control, 1, row);
    }

    private static void SetNumber(NumericUpDown control, int value) =>
        control.Value = Math.Max(control.Minimum, Math.Min(control.Maximum, value));

    private sealed class ActionChoice
    {
        public SheepAction Value { get; }
        public ActionChoice(SheepAction value) => Value = value;
        public override string ToString() => Value switch
        {
            SheepAction.Normal => "自由活动", SheepAction.Run => "奔跑",
            SheepAction.Walk => "走路", SheepAction.Handstand => "倒立",
            SheepAction.Bow => "鞠躬", SheepAction.Sleep => "睡觉",
            SheepAction.Blink => "眨眼", SheepAction.Turn => "转身",
            SheepAction.Collision => "碰撞", SheepAction.Pee => "嘘嘘",
            SheepAction.Yawn => "打哈欠", SheepAction.Baa => "咩咩叫",
            SheepAction.Sneeze => "打喷嚏", SheepAction.Amazed => "惊讶",
            SheepAction.Frightened => "受惊", SheepAction.Flower => "喂一朵花",
            SheepAction.Sit => "坐下", SheepAction.Blush => "脸红",
            SheepAction.Roll => "翻滚", SheepAction.Backflip => "后空翻",
            SheepAction.Spin => "旋转", SheepAction.Fall => "从空中落下",
            SheepAction.Jump => "跳跃", SheepAction.BlackSheep => "遇见黑羊",
            SheepAction.BlackSheepMeeting => "黑羊相会", SheepAction.BlackSheepChase => "黑羊追逐",
            SheepAction.Ufo => "UFO 事件", SheepAction.UfoVisitor => "UFO 来访",
            SheepAction.UfoChase => "UFO 追逐", SheepAction.Bathtub => "燃烧与浴盆",
            _ => Value.ToString()
        };
    }
}
