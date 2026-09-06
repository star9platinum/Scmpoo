using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using Scmpoo.Modern.Rendering;

namespace Scmpoo.Modern.Tests;

public static class SpriteAtlasTests
{
    public static void Run(Assembly resources, string outputDirectory)
    {
        using SpriteAtlas atlas = new(resources);
        string[] resourceNames = resources.GetManifestResourceNames();
        using Bitmap contactSheet = new(16 * 44, 11 * 44, PixelFormat.Format32bppArgb);
        using Graphics graphics = Graphics.FromImage(contactSheet);
        graphics.Clear(Color.FromArgb(225, 230, 234));
        for (int sheetIndex = 0; sheetIndex < 11; sheetIndex++)
        {
            string suffix = (101 + sheetIndex).ToString(System.Globalization.CultureInfo.InvariantCulture) + ".bmp";
            string? resourceName = null;
            foreach (string name in resourceNames)
            {
                if (name.Equals(suffix, StringComparison.OrdinalIgnoreCase) || name.EndsWith("." + suffix, StringComparison.OrdinalIgnoreCase))
                    resourceName = name;
            }
            if (resourceName is null) throw new InvalidOperationException("Embedded sheet missing: " + suffix);
            using Stream stream = resources.GetManifestResourceStream(resourceName)
                ?? throw new InvalidOperationException("Embedded sheet missing: " + suffix);
            using Bitmap reference = new(stream);
            int transparentColor = reference.GetPixel(0, reference.Height - 1).ToArgb();
            for (int frame = 0; frame < 16; frame++)
            {
                int sprite = sheetIndex * 16 + frame;
                Bitmap bitmap = atlas.GetBitmap(sprite);
                Bitmap mirrored = atlas.GetBitmap(sprite + 256);
                using Region region = atlas.CreateRegion(sprite);
                Assert(ReferenceEquals(bitmap, atlas.GetBitmap(sprite)), "Bitmap cache did not reuse a sprite.");
                for (int y = 0; y < 40; y++)
                {
                    for (int x = 0; x < 40; x++)
                    {
                        int expected = reference.GetPixel(frame * 40 + x, y).ToArgb();
                        if (expected == transparentColor) expected = 0;
                        int actual = bitmap.GetPixel(x, y).ToArgb();
                        Assert(actual == expected, "RLE8 decode mismatch in sprite " + sprite + " at " + x + "," + y);
                        Assert(actual == mirrored.GetPixel(39 - x, y).ToArgb(), "Mirror mismatch in sprite " + sprite);
                        Assert(region.IsVisible(x, y) == (actual != 0), "Region mismatch in sprite " + sprite + " at " + x + "," + y);
                    }
                }
                graphics.DrawImageUnscaled(bitmap, frame * 44 + 2, sheetIndex * 44 + 2);
            }
        }

        Bitmap original = atlas.GetBitmap(3);
        Bitmap enlarged = atlas.GetBitmap(3, 4);
        Assert(enlarged.Width == 160 && enlarged.Height == 160, "Scale dimensions are incorrect.");
        for (int y = 0; y < 160; y++)
        {
            for (int x = 0; x < 160; x++)
                Assert(enlarged.GetPixel(x, y).ToArgb() == original.GetPixel(x / 4, y / 4).ToArgb(), "Integer scale changed a pixel.");
        }
        using (Region first = atlas.CreateRegion(3)) first.MakeEmpty();
        using (Region next = atlas.CreateRegion(3)) Assert(!next.IsEmpty(graphics), "A caller modified the cached region.");
        using (Region beam = atlas.CreateRegion(3, 2, 0, false, 100))
        {
            Assert(beam.IsVisible(1, 81), "Beam was not included below the sprite.");
            Assert(!beam.IsVisible(1, 281), "Beam extends beyond its height.");
        }

        int previousCount = CountOpaque(atlas.GetBitmap(158));
        Assert(previousCount > 0, "Fade test sprite is empty.");
        for (int step = 1; step <= 9; step++)
        {
            int count = CountOpaque(atlas.GetBitmap(158, 1, step));
            Assert(count <= previousCount, "Fade unexpectedly made pixels visible again.");
            previousCount = count;
        }
        Assert(previousCount == 0, "Final fade step did not clear the sprite.");
        Directory.CreateDirectory(outputDirectory);
        contactSheet.Save(Path.Combine(outputDirectory, "sprite-atlas-contact-sheet.png"), ImageFormat.Png);
        Console.WriteLine("Sprite atlas: 176 originals, 176 mirrors, RLE8 reference pixels, regions, cache, scale, fade passed.");
    }

    private static int CountOpaque(Bitmap bitmap)
    {
        int count = 0;
        for (int y = 0; y < bitmap.Height; y++)
            for (int x = 0; x < bitmap.Width; x++)
                if (bitmap.GetPixel(x, y).A != 0) count++;
        return count;
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
