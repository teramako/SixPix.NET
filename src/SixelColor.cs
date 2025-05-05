#if IMAGESHARP4 // ImageSharp v4.0 adds support for CUR and ICO files
using System.Numerics;
#endif
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace SixPix;

/// <summary>
/// Color structure for Sixel
/// </summary>
public record struct SixelColor
{
    /// <summary>
    /// The red component. The value is between 0 and 100
    /// </summary>
    public byte R { get; private set; }
    /// <summary>
    /// The green component. The value is between 0 and 100
    /// </summary>
    public byte G { get; private set; }
    /// <summary>
    /// The blue component. The value is between 0 and 100
    /// </summary>
    public byte B { get; private set; }
    /// <summary>
    /// The alpha component. The value is between 0 and 100
    /// </summary>
    /// <remarks>
    /// Not normally used.
    /// </remarks>
    public byte A { get; private set; }

    public SixelColor(byte r, byte g, byte b, byte a = 100)
    {
        if (r > 100)
            throw new ArgumentOutOfRangeException(nameof(r), r, "should be between 0 and 100");
        if (g > 100)
            throw new ArgumentOutOfRangeException(nameof(g), g, "should be between 0 and 100");
        if (b > 100)
            throw new ArgumentOutOfRangeException(nameof(b), b, "should be between 0 and 100");
        if (a > 100)
            throw new ArgumentOutOfRangeException(nameof(a), a, "should be between 0 and 100");

        if (a == 0)
            (R, G, B, A) = (0, 0, 0, a);
        else
            (R, G, B, A) = (r, g, b, a);
    }

    /// <summary>
    /// Create SixelColor from RGB values
    /// </summary>
    public static SixelColor FromRgb(int r, int g, int b)
    {
        return new((byte)r, (byte)g, (byte)b);
    }

    /// <summary>
    /// Create SixelColor from HLS values
    /// </summary>
    public static SixelColor FromHLS(int h, int l, int s)
    {
        double r; double g; double b;
        double max; double min;

        if (l > 50)
        {
            max = l + (s * (1.0 - (l / 100.0)));
            min = l - (s * (1.0 - (l / 100.0)));
        }
        else
        {
            max = l + (s * l / 100.0);
            min = l - (s * l / 100.0);
        }

        h = (h + 240) % 360;

        (r, g, b) = h switch
        {
            < 60 => (max, min + ((max - min) * h / 60.0), min),
            < 120 => (min + ((max - min) * (120 - h) / 60.0), max, min),
            < 180 => (min, max, min + ((max - min) * (h - 120) / 60.0)),
            < 240 => (min, min + ((max - min) * (240 - h) / 60.0), max),
            < 300 => (min + ((max - min) * (h - 240) / 60.0), min, max),
            _ => (max, min, min + ((max - min) * (360 - h) / 60.0))
        };

        return new((byte)Math.Round(r),
                   (byte)Math.Round(g),
                   (byte)Math.Round(b));
    }

    public static SixelColor FromRgba32(Rgba32 rgba,
                                        Transparency transp = Transparency.Default,
                                        Color? tc = null,
                                        Color? bg = null)
    {
        var alpha = (byte)Math.Round(rgba.A * 100.0 / 0xFF);
        if (alpha == 0)
            return new(0, 0, 0, 0);
#if IMAGESHARP4 // ImageSharp v4.0
        else if (tc is not null && tc == Color.FromScaledVector(new Vector4(rgba.R, rgba.G, rgba.B, 0)))
            return new(0, 0, 0, alpha);
        else if (transp == Transparency.Background && bg is not null && bg == Color.FromScaledVector(new Vector4(rgba.R, rgba.G, rgba.B, 0)))
            return new(0, 0, 0, alpha);
#else
        else if (tc is not null && tc == Color.FromRgb(rgba.R, rgba.G, rgba.B))
            return new(0, 0, 0, alpha);
        else if (transp == Transparency.Background && bg is not null && bg == Color.FromRgb(rgba.R, rgba.G, rgba.B))
            return new(0, 0, 0, alpha);
#endif
        else
        {
            return new((byte)Math.Round(rgba.R * 100.0 / 0xFF),
                       (byte)Math.Round(rgba.G * 100.0 / 0xFF),
                       (byte)Math.Round(rgba.B * 100.0 / 0xFF),
                       alpha);
        }
    }

    public static SixelColor FromColor(Color color,
                                       Transparency transp = Transparency.Default,
                                       Color? tc = null,
                                       Color? bg = null)
    {
        return FromRgba32(color.ToPixel<Rgba32>(), transp, tc, bg);
    }

    /// <summary>
    /// Convert to <see cref="Rgba32"/>
    /// </summary>
    public readonly Rgba32 ToRgba32()
    {
        return new((byte)Math.Round(R * 0xFF / 100.0),
                   (byte)Math.Round(G * 0xFF / 100.0),
                   (byte)Math.Round(B * 0xFF / 100.0),
                   (byte)Math.Round(A * 0xFF / 100.0));
    }

    public readonly ReadOnlySpan<char> ToColorPalette()
    {
        return $"{R:d};{G:d};{B:d}";
    }
}
