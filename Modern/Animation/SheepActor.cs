using System;
using System.Drawing;

namespace Scmpoo.Modern.Animation;

public sealed partial class SheepActor
{
    private const int NoCollision = int.MinValue;
    private const int BelowFloor = int.MinValue + 1;
    private readonly IAnimationEnvironment _environment;
    private readonly Random _random;
    private IntPtr _landingWindow;
    private EngineRect _landingRect;
    private Rectangle _sceneArea;
    private SpriteFrame _mainFrame;
    private SpriteFrame _companionFrame;
    private bool _companionVisible;
    private bool _dragging;
    private Point _lastPointer;
    private int _idleTicks;
    private int _chimesRemaining;
    private long _nextChimeTime;
    private long _lastChimeHour = -1;

    public SheepActor(IAnimationEnvironment environment, int seed, AnimationOptions? options = null)
    {
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
        _random = new Random(seed);
        Options = options ?? new AnimationOptions();
        _sceneArea = environment.GetWorkArea(environment.PointerPosition);
        _lastPointer = environment.PointerPosition;
        _x = _sceneArea.Left + Math.Max(0, _sceneArea.Width - 40) / 2;
        _y = _sceneArea.Top + Math.Max(0, _sceneArea.Height - 40) / 2;
        _state = Options.Gravity ? 96 : 11;
        _sprite = 3;
        PresentMain(_x, _y, _sprite);
    }

    public AnimationOptions Options { get; set; }
    public IntPtr MainWindowHandle { get; set; }
    public IntPtr CompanionWindowHandle { get; set; }
    public int State => _state;
    public int X => _x;
    public int Y => _y;
    public bool IsDragging => _dragging;
    public bool IsSleeping => _sleeping != 0;
    public bool IsSpecialAction => _state >= 116 && _state <= 152;
    public bool MainAboveCompanion => _mainAboveCompanion != 0;
    public bool RetainCompanionFrame => _retainCompanion != 0;
    public SpriteFrame MainFrame => new(_mainFrame.X, _mainFrame.Y, _mainFrame.SpriteIndex,
        _beamHeight, 0, _mainFrame.Visible);
    public SpriteFrame CompanionFrame => new(_companionFrame.X, _companionFrame.Y,
        _companionFrame.SpriteIndex, _companionBeamHeight, _fadeStep,
        _companionVisible && _companionFrame.Visible);

    // The scheduler supplies one original simulation period. Rendering can run
    // independently, and no timer or platform handle is owned by this actor.
    public void Tick()
    {
        if (_dragging) return;
        Point pointer = _environment.PointerPosition;
        bool active = pointer != _lastPointer || Options.AlwaysMoving;
        _lastPointer = pointer;
        if (active)
        {
            _idleTicks = 0;
            _pendingSleepState = 0;
            if (_sleeping != 0)
            {
                _sleeping = 0;
                React(0);
            }
        }
        else if (_sleeping == 0 && ++_idleTicks > 300)
        {
            _pendingSleepState = 113;
        }
        if (IsSpecialEntry(_state))
            _sceneArea = _environment.GetWorkArea(new Point(_x + 20, _y + 20));
        AdvanceState();
    }

    public void StartAction(SheepAction action) => StartState((int)action);

    public void ApplyOptions(AnimationOptions options)
    {
        if (options == null) throw new ArgumentNullException(nameof(options));
        bool gravityChanged = Options.Gravity != options.Gravity;
        Options = options.Clone();
        Options.SpecialFrequencyPercent = Math.Max(0, Math.Min(500, Options.SpecialFrequencyPercent));
        if (!Options.Sound) StopSound();
        if (!Options.Chime && _chimesRemaining > 0)
        {
            _chimesRemaining = 0;
            StopSound();
            if (_state is 81 or 82) _state = _sleeping != 0 ? 113 : 1;
        }
        if (Options.AlwaysMoving)
        {
            _idleTicks = 0;
            _pendingSleepState = 0;
            if (_sleeping != 0)
            {
                _sleeping = 0;
                React(0);
            }
        }
        if (gravityChanged && !IsSpecialAction && !_companionVisible && !_dragging)
        {
            _gravityEnabled = Options.Gravity ? 1 : 0;
            _landingWindow = IntPtr.Zero;
            _landingRect = default;
            _verticalSpeed = 0;
            _horizontalSpeed = 0;
            _hasBounced = 0;
            _state = Options.Gravity ? 96 : 1;
        }
    }

    public void StartState(int state)
    {
        if (state < 0 || state > 154) throw new ArgumentOutOfRangeException(nameof(state));
        StopSound();
        HideCompanion();
        _beamHeight = 0;
        _periodCounter = 0;
        _frameCounter = 0;
        _actionVariant = 0;
        _durationCounter = 0;
        _chimesRemaining = 0;
        _pendingSleepState = 0;
        _sleeping = 0;
        _idleTicks = 0;
        _dragging = false;
        _state = state;
        if (IsSpecialEntry(state))
            _sceneArea = _environment.GetWorkArea(new Point(_x + 20, _y + 20));
    }

    public void DragTo(int x, int y)
    {
        if (!_dragging)
        {
            StopSound();
            HideCompanion();
            _beamHeight = 0;
            _sprite = 4;
            _sleeping = 0;
        }
        _dragging = true;
        _x = x;
        _y = y;
        _idleTicks = 0;
        PresentMain(x, y, _sprite);
    }

    public void EndDrag()
    {
        if (!_dragging) return;
        _dragging = false;
        _verticalSpeed = 0;
        _horizontalSpeed = 0;
        _pendingSleepState = 0;
        _state = Options.Gravity ? 96 : 11;
    }

    public void MoveTo(int x, int y)
    {
        _x = x;
        _y = y;
        PresentMain(x, y, _sprite);
    }

    // Called after display removal or a monitor preference change, not during
    // intentional off-screen entrances in the original special sequences.
    public void RecoverToVisibleMonitor()
    {
        Rectangle area = _environment.GetWorkArea(new Point(_x + 20, _y + 20));
        if (SpriteOnMonitor(_x, _y)) return;
        StartState(Options.Gravity ? 96 : 11);
        MoveTo(Math.Max(area.Left, Math.Min(_x, area.Right - 40)),
            Math.Max(area.Top, Math.Min(_y, area.Bottom - 40)));
    }

    private int GravityOption => Options.Gravity ? 1 : 0;
    private Rectangle SceneBounds => IsSpecialAction ? _sceneArea : _environment.DesktopBounds;
    private int SceneLeft => SceneBounds.Left;
    private int SceneTop => SceneBounds.Top;
    private int SceneRight => SceneBounds.Right;
    private int SceneBottom => SceneBounds.Bottom;
    private int SceneWidth => Math.Max(80, SceneBounds.Width);
    private int SceneHeight => Math.Max(80, SceneBounds.Height);
    private int SceneCenterX => SceneLeft + SceneWidth / 2;
    private int ScenePointX(int numerator, int denominator) => SceneLeft + SceneWidth * numerator / denominator;
    private int ScenePointY(int numerator, int denominator) => SceneTop + SceneHeight * numerator / denominator;
    private int NextRandom() => _random.Next(32768);
    private static bool IsSpecialEntry(int state) => state is 116 or 121 or 126 or 128 or 135 or 142 or 147;

    private bool ShouldTriggerSpecial(int denominator)
    {
        int frequency = Math.Max(0, Math.Min(500, Options.SpecialFrequencyPercent));
        return frequency != 0 && _random.Next(denominator * 100) < frequency;
    }

    private EngineRect RandomMonitorRect()
    {
        var monitors = _environment.MonitorWorkAreas;
        return new EngineRect(monitors.Count == 0 ? _environment.DesktopBounds : monitors[_random.Next(monitors.Count)]);
    }

    private bool GetMonitorRect(int x, int y, out EngineRect result)
    {
        result = new EngineRect(_environment.GetWorkArea(new Point(x, y)));
        return true;
    }

    private bool SpriteOnMonitor(int x, int y)
    {
        Rectangle sprite = new(x, y, 40, 40);
        foreach (Rectangle monitor in _environment.MonitorWorkAreas)
            if (monitor.IntersectsWith(sprite)) return true;
        return false;
    }

    private int RandomWindowX(EngineRect rect)
    {
        int width = Math.Max(1, rect.Right - rect.Left);
        return rect.Left + width / 3 + NextRandom() % Math.Max(1, width / 3) - 20;
    }

    private void GetWalkBounds(int x, int y, out EngineRect bounds)
    {
        bounds = new EngineRect(_environment.GetWorkArea(new Point(x + 20, y + 20)));
        // Merge only monitors connected at this height. Empty gaps remain edges.
        bool expanded;
        do
        {
            expanded = false;
            foreach (Rectangle monitor in _environment.MonitorWorkAreas)
            {
                if (y + 20 < monitor.Top || y + 20 >= monitor.Bottom ||
                    monitor.Right < bounds.Left || monitor.Left > bounds.Right) continue;
                int left = Math.Min(bounds.Left, monitor.Left);
                int right = Math.Max(bounds.Right, monitor.Right);
                if (left != bounds.Left || right != bounds.Right)
                {
                    bounds.Left = left;
                    bounds.Right = right;
                    expanded = true;
                }
            }
        } while (expanded);
    }

    private IntPtr GetForegroundWindow() => _environment.ForegroundWindow;
    private IntPtr WindowAt(Point point) => _environment.WindowAt(point);
    private bool IsSheepWindow(IntPtr handle) => _environment.IsSheepWindow(handle);
    private bool IsSupportValid(IntPtr handle) => handle == IntPtr.Zero || _environment.IsWindow(handle);
    private void RefreshWindowSnapshot() { }

    private void GetSupportRect(IntPtr handle, out EngineRect result)
    {
        if (handle != IntPtr.Zero)
        {
            result = new EngineRect(_environment.GetWindowBounds(handle));
            return;
        }
        result = new EngineRect(_environment.GetWorkArea(new Point(_x + 20, _y + 20)));
        result.Top = result.Bottom;
        result.Bottom += SceneHeight;
    }

    private void RaiseWindow(IntPtr handle) => _environment.BringToFront(this,
        handle == CompanionWindowHandle && _companionVisible);

    private void PlaceWindowBehind(IntPtr handle, IntPtr target) => _environment.PlaceBehind(this,
        handle == CompanionWindowHandle && _companionVisible, target);

    private void ShowCompanion()
    {
        if (_companionVisible) return;
        _companionVisible = true;
        _fadeStep = 0;
        _companionBeamHeight = 0;
        _retainCompanion = 0;
        _companionFrame = new SpriteFrame(0, 0, 3, visible: false);
    }

    private void HideCompanion()
    {
        _companionVisible = false;
        _fadeStep = 0;
        _companionBeamHeight = 0;
        _retainCompanion = 0;
    }

    private void PresentMain(int x, int y, int sprite)
    {
        int frame = sprite >= 9 && sprite <= 14 || _direction > 0 ? sprite : sprite + 256;
        _mainFrame = new SpriteFrame(x, y, frame, _beamHeight);
    }

    private void PresentCompanion(int x, int y, int sprite)
    {
        int frame = sprite >= 9 && sprite <= 14 || _companionDirection > 0 ? sprite : sprite + 256;
        _companionFrame = new SpriteFrame(x, y, frame, _companionBeamHeight, _fadeStep);
    }

    private void PlaySound(int resource, int flags, int unused)
    {
        if (Options.Sound) _environment.PlaySound(this, resource, (flags & 8) != 0);
    }

    private void StopSound() => _environment.StopSound(this);

    private void UpdateChime()
    {
        long now = _environment.TimestampMilliseconds;
        // Completion is independent of sound and of the setting at this tick.
        // Disabling the alarm during a chime cannot strand states 81/82.
        if (_chimesRemaining > 0)
        {
            if (!Options.Chime) _chimesRemaining = 1;
            if (now >= _nextChimeTime || !Options.Chime)
            {
                _nextChimeTime = now + 1000;
                if (--_chimesRemaining > 0)
                {
                    if (Options.Sound) _environment.PlaySound(this, 108, false);
                }
                else if (_state is 81 or 82)
                {
                    _state = _sleeping != 0 ? 113 : 1;
                }
            }
            return;
        }
        if (_state is 81 or 82)
        {
            _state = 1;
        }
        DateTime clock = _environment.Now;
        long hourKey = clock.Ticks / TimeSpan.TicksPerHour;
        if (!Options.Chime || clock.Minute != 0 || hourKey == _lastChimeHour) return;
        _lastChimeHour = hourKey;
        int hour = clock.Hour % 12;
        if (hour == 0) hour = 12;
        _chimesRemaining = hour + 1;
        _nextChimeTime = now;
        HideCompanion();
        _beamHeight = 0;
        _state = 81;
    }

    private int FindPeerEdge(int fromX, int toX, int top, int bottom)
    {
        foreach (SheepActor sheep in _environment.Sheep)
        {
            if (ReferenceEquals(sheep, this) || sheep.IsSpecialAction) continue;
            int left = sheep.X;
            int right = left + 40;
            int peerTop = sheep.Y;
            int peerBottom = peerTop + 40;
            if (peerTop >= bottom || peerBottom <= top) continue;
            if (toX > fromX && right > fromX && right <= toX) return right;
            if (toX < fromX && left >= toX && left < fromX) return left;
        }
        return NoCollision;
    }

    private int FindSideEdge(out IntPtr window, int top, int bottom, int nextX, int oldX)
    {
        window = IntPtr.Zero;
        var windows = _environment.Windows;
        for (int i = 0; i < windows.Count; i++)
        {
            DesktopWindow candidate = windows[i];
            Rectangle rect = candidate.Bounds;
            bool leftward = oldX > nextX;
            int edge = leftward ? rect.Right : rect.Left;
            if (rect.Top >= top || rect.Bottom <= bottom ||
                (leftward ? edge < nextX || edge >= oldX : edge > nextX || edge <= oldX)) continue;
            bool covered = false;
            for (int j = 0; j < i; j++)
            {
                Rectangle cover = windows[j].Bounds;
                if (cover.Top <= top && cover.Bottom >= bottom &&
                    cover.Left <= Math.Min(nextX, oldX) && cover.Right >= Math.Max(nextX, oldX))
                {
                    covered = true;
                    break;
                }
            }
            if (covered || !_environment.IsWindow(candidate.Handle)) continue;
            Rectangle live = _environment.GetWindowBounds(candidate.Handle);
            if ((leftward ? live.Right : live.Left) != edge) continue;
            window = candidate.Handle;
            return edge;
        }
        return NoCollision;
    }

    private int FindLandingEdge(out IntPtr window, int bottom, int previousBottom, int left, int right)
    {
        window = IntPtr.Zero;
        var windows = _environment.Windows;
        for (int i = 0; i < windows.Count; i++)
        {
            DesktopWindow candidate = windows[i];
            Rectangle rect = candidate.Bounds;
            if (rect.Top > bottom || rect.Top <= previousBottom ||
                rect.Left >= right || rect.Right <= left || rect.Top <= SceneTop + 10) continue;
            bool covered = false;
            for (int j = 0; j < i; j++)
            {
                Rectangle cover = windows[j].Bounds;
                if (cover.Left <= left && cover.Right >= right && cover.Top <= previousBottom && cover.Bottom >= bottom)
                {
                    covered = true;
                    break;
                }
            }
            if (covered || !_environment.IsWindow(candidate.Handle)) continue;
            window = candidate.Handle;
            return rect.Top;
        }
        Rectangle floor = _environment.GetWorkArea(new Point((left + right) / 2, previousBottom));
        if (bottom >= floor.Bottom && previousBottom <= floor.Bottom) return floor.Bottom;
        return NoCollision;
    }

    private int FindSpecificLandingEdge(IntPtr window, int bottom, int previousBottom, int left, int right)
    {
        if (_environment.IsWindow(window))
        {
            Rectangle rect = _environment.GetWindowBounds(window);
            if (rect.Top <= bottom && rect.Top > previousBottom && rect.Left < right && rect.Right > left)
                return rect.Top;
        }
        Rectangle floor = _environment.GetWorkArea(new Point((left + right) / 2, previousBottom));
        return bottom > floor.Bottom ? BelowFloor : NoCollision;
    }

    private struct EngineRect
    {
        public int Left, Top, Right, Bottom;
        public EngineRect(Rectangle rect)
        {
            Left = rect.Left;
            Top = rect.Top;
            Right = rect.Right;
            Bottom = rect.Bottom;
        }
    }
}
