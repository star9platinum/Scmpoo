using System;
using System.Collections.Generic;
using System.Drawing;

namespace Scmpoo.Modern.Animation;

public sealed class AnimationOptions
{
    public bool Sound { get; set; } = true;
    public bool Chime { get; set; }
    public bool Gravity { get; set; }
    public bool AlwaysMoving { get; set; }
    public int SpecialFrequencyPercent { get; set; } = 100;

    public AnimationOptions Clone() => (AnimationOptions)MemberwiseClone();
}

public enum SheepAction
{
    Normal = 1, Run = 7, Walk = 11, Handstand = 13, Bow = 15,
    Sleep = 17, Blink = 20, Turn = 24, Collision = 30, Pee = 35,
    Yawn = 43, Baa = 45, Sneeze = 47, Amazed = 49, Frightened = 51,
    Flower = 53, Sit = 58, Blush = 62, Roll = 65, Backflip = 69,
    Spin = 75, Fall = 96, Jump = 9, BlackSheep = 116,
    BlackSheepMeeting = 121, BlackSheepChase = 126,
    Ufo = 128, UfoVisitor = 135, UfoChase = 142, Bathtub = 147
}

public readonly struct SpriteFrame
{
    public int X { get; }
    public int Y { get; }
    public int SpriteIndex { get; }
    public int BeamHeight { get; }
    public int FadeStep { get; }
    public bool Visible { get; }
    public bool BathtubOverlay { get; }

    public SpriteFrame(int x, int y, int spriteIndex, int beamHeight = 0,
        int fadeStep = 0, bool visible = true, bool bathtubOverlay = false)
    {
        X = x;
        Y = y;
        SpriteIndex = spriteIndex;
        BeamHeight = Math.Max(0, beamHeight);
        FadeStep = Math.Max(0, Math.Min(9, fadeStep));
        Visible = visible;
        BathtubOverlay = bathtubOverlay;
    }
}

public readonly struct DesktopWindow
{
    public IntPtr Handle { get; }
    public Rectangle Bounds { get; }

    public DesktopWindow(IntPtr handle, Rectangle bounds)
    {
        Handle = handle;
        Bounds = bounds;
    }
}

// All platform access is supplied by the host, so the complete animation
// machine also runs deterministically in headless tests.
public interface IAnimationEnvironment
{
    Rectangle DesktopBounds { get; }
    IList<Rectangle> MonitorWorkAreas { get; }
    IList<DesktopWindow> Windows { get; }
    IList<SheepActor> Sheep { get; }
    Point PointerPosition { get; }
    DateTime Now { get; }
    long TimestampMilliseconds { get; }
    IntPtr ForegroundWindow { get; }
    bool IsWindow(IntPtr window);
    bool IsSheepWindow(IntPtr window);
    Rectangle GetWindowBounds(IntPtr window);
    Rectangle GetWorkArea(Point point);
    IntPtr WindowAt(Point point);
    void BringToFront(SheepActor sheep, bool companion);
    void PlaceBehind(SheepActor sheep, bool companion, IntPtr window);
    void PlaySound(SheepActor sheep, int resourceId, bool loop);
    void StopSound(SheepActor sheep);
}
