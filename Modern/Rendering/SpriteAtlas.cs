using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Scmpoo.Modern.Rendering;

// One atlas is shared by the whole flock. Returned bitmaps remain atlas-owned;
// regions are cloned because a WinForms window owns and disposes its Region.
public sealed class SpriteAtlas : IDisposable
{
    public const int SpriteSize = 40;
    public const int FrameCount = 176;
    public const int MaximumScale = 4;

    private readonly int[][] frames = new int[FrameCount][];
    private readonly Dictionary<int, CachedFrame> cache = new();
    private bool disposed;

    public SpriteAtlas() : this(Assembly.GetExecutingAssembly()) { }

    public SpriteAtlas(Assembly assembly)
    {
        if (assembly is null) throw new ArgumentNullException(nameof(assembly));
        string[] names = assembly.GetManifestResourceNames();
        for (int sheetIndex = 0; sheetIndex < 11; sheetIndex++)
        {
            string suffix = (sheetIndex + 101).ToString(System.Globalization.CultureInfo.InvariantCulture) + ".bmp";
            string? resourceName = null;
            foreach (string name in names)
            {
                if (name.Equals(suffix, StringComparison.OrdinalIgnoreCase) ||
                    name.EndsWith("." + suffix, StringComparison.OrdinalIgnoreCase))
                {
                    resourceName = name;
                    break;
                }
            }
            if (resourceName is null) throw new InvalidDataException("Missing embedded sprite sheet: " + suffix);
            using Stream stream = assembly.GetManifestResourceStream(resourceName)
                ?? throw new InvalidDataException("Missing embedded sprite sheet: " + suffix);
            DecodedSheet sheet = ReadSheet(stream);
            for (int frame = 0; frame < 16; frame++)
            {
                int[] pixels = new int[SpriteSize * SpriteSize];
                for (int y = 0; y < SpriteSize; y++)
                {
                    for (int x = 0; x < SpriteSize; x++)
                    {
                        int color = sheet.Pixels[y * sheet.Width + frame * SpriteSize + x];
                        pixels[y * SpriteSize + x] = color == sheet.TransparentColor ? 0 : color;
                    }
                }
                frames[sheetIndex * 16 + frame] = pixels;
            }
        }
    }

    public int CachedFrameCount => cache.Count;

    public Bitmap GetBitmap(int spriteIndex, int scale = 1, int fadeStep = 0, bool bathtubOverlay = false)
        => GetFrame(spriteIndex, scale, fadeStep).Bitmap;

    public Region CreateRegion(int spriteIndex, int scale = 1, int fadeStep = 0,
        bool bathtubOverlay = false, int beamHeight = 0)
    {
        Region region = GetFrame(spriteIndex, scale, fadeStep).Region.Clone();
        if (beamHeight > 0)
        {
            region.Union(new Rectangle(0, SpriteSize * scale, SpriteSize * scale, checked(beamHeight * scale)));
        }
        return region;
    }

    private CachedFrame GetFrame(int spriteIndex, int scale, int fadeStep)
    {
        if (disposed) throw new ObjectDisposedException(nameof(SpriteAtlas));
        int sourceIndex = spriteIndex >= 256 ? spriteIndex - 256 : spriteIndex;
        if (sourceIndex < 0 || sourceIndex >= FrameCount) throw new ArgumentOutOfRangeException(nameof(spriteIndex));
        if (scale < 1 || scale > MaximumScale) throw new ArgumentOutOfRangeException(nameof(scale));
        if (fadeStep < 0 || fadeStep > 9) throw new ArgumentOutOfRangeException(nameof(fadeStep));
        int key = spriteIndex | ((scale - 1) << 9) | (fadeStep << 11);
        if (cache.TryGetValue(key, out CachedFrame? cached)) return cached;

        int[] pixels = BuildPixels(sourceIndex, spriteIndex >= 256, fadeStep);
        int width = SpriteSize * scale;
        int[] scaledPixels = new int[width * width];
        Region region = new();
        region.MakeEmpty();
        for (int y = 0; y < SpriteSize; y++)
        {
            int runStart = -1;
            for (int x = 0; x <= SpriteSize; x++)
            {
                bool opaque = x < SpriteSize && pixels[y * SpriteSize + x] != 0;
                if (opaque && runStart < 0) runStart = x;
                if (!opaque && runStart >= 0)
                {
                    region.Union(new Rectangle(runStart * scale, y * scale, (x - runStart) * scale, scale));
                    runStart = -1;
                }
                if (x == SpriteSize) continue;
                for (int dy = 0; dy < scale; dy++)
                {
                    for (int dx = 0; dx < scale; dx++)
                        scaledPixels[(y * scale + dy) * width + x * scale + dx] = pixels[y * SpriteSize + x];
                }
            }
        }

        Bitmap bitmap = new(width, width, PixelFormat.Format32bppArgb);
        try
        {
            BitmapData data = bitmap.LockBits(new Rectangle(0, 0, width, width), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            try
            {
                for (int y = 0; y < width; y++)
                    Marshal.Copy(scaledPixels, y * width, new IntPtr(data.Scan0.ToInt64() + (long)y * data.Stride), width);
            }
            finally { bitmap.UnlockBits(data); }
            cached = new CachedFrame(bitmap, region);
            cache.Add(key, cached);
            return cached;
        }
        catch
        {
            bitmap.Dispose();
            region.Dispose();
            throw;
        }
    }

    private int[] BuildPixels(int sourceIndex, bool mirror, int fadeStep)
    {
        int[] source = frames[sourceIndex];
        int[] result = new int[SpriteSize * SpriteSize];
        if (fadeStep == 9) return result;
        int[] mask = frames[172];
        for (int y = 0; y < SpriteSize; y++)
        {
            for (int x = 0; x < SpriteSize; x++)
            {
                int pixel = source[y * SpriteSize + (mirror ? SpriteSize - 1 - x : x)];
                // The original fade is cumulative shifted mask removal, not alpha fading.
                for (int step = 0; step < fadeStep && pixel != 0; step++)
                {
                    if (x >= step && y >= step && mask[(y - step) * SpriteSize + x - step] != 0)
                        pixel = 0;
                }
                result[y * SpriteSize + x] = pixel;
            }
        }
        return result;
    }

    // GDI+ expands the shipped RLE8 BMPs to RGB. Their bottom-left background
    // color is the original transparency key and is shared by every sheet frame.
    private static DecodedSheet ReadSheet(Stream stream)
    {
        if (stream is null) throw new InvalidDataException("Missing sprite data.");
        using Bitmap sheet = new(stream);
        if (sheet.Width != SpriteSize * 16 || sheet.Height != SpriteSize)
            throw new InvalidDataException("Unsupported sprite sheet format.");
        int[] pixels = new int[sheet.Width * sheet.Height];
        BitmapData data = sheet.LockBits(new Rectangle(0, 0, sheet.Width, sheet.Height),
            ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        try
        {
            for (int y = 0; y < sheet.Height; y++)
                Marshal.Copy(new IntPtr(data.Scan0.ToInt64() + (long)y * data.Stride), pixels, y * sheet.Width, sheet.Width);
        }
        finally { sheet.UnlockBits(data); }
        int transparentColor = pixels[(sheet.Height - 1) * sheet.Width];
        return new DecodedSheet(sheet.Width, pixels, transparentColor);
    }

    public void Dispose()
    {
        if (disposed) return;
        foreach (CachedFrame frame in cache.Values)
        {
            frame.Bitmap.Dispose();
            frame.Region.Dispose();
        }
        cache.Clear();
        disposed = true;
    }

    private sealed class CachedFrame(Bitmap bitmap, Region region)
    {
        public Bitmap Bitmap { get; } = bitmap;
        public Region Region { get; } = region;
    }

    private sealed class DecodedSheet(int width, int[] pixels, int transparentColor)
    {
        public int Width { get; } = width;
        public int[] Pixels { get; } = pixels;
        public int TransparentColor { get; } = transparentColor;
    }
}
