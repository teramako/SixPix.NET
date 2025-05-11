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

    public static SixelColor FromRgba32(Rgba32 rgba)
    {
        return new((byte)Math.Round(rgba.R * 100.0 / 0xFF),
                   (byte)Math.Round(rgba.G * 100.0 / 0xFF),
                   (byte)Math.Round(rgba.B * 100.0 / 0xFF),
                   (byte)Math.Round(rgba.A * 100.0 / 0xFF));
    }

    public static SixelColor FromRgba32(Rgba32 rgba,
                                        Transparency transp,
                                        Rgba32? tc = null,
                                        Rgba32? bg = null)
    {
        SixelColor color;
        if (rgba.A == 0)
        {
            color = transp switch
            {
                Transparency.None => FromRgba32(Sixel.BackgroundColor.ToPixel<Rgba32>()),
                Transparency.TopLeft => FromRgba32(Sixel.BackgroundColor.ToPixel<Rgba32>()),
                Transparency.Background => bg is not null
                                           ? FromRgba32(bg.Value)
                                           : default,
                _ => default
            };
        }
        else if (tc is not null && tc == rgba)
            color = default;
        else if (transp == Transparency.Background && bg is not null && bg == rgba)
            color = default;
        else
            color = FromRgba32(rgba);

        if (color.A is > 0 and < 100)
        {
            // Blend the background color to create opaque color
            color.Blend(transp == Transparency.None
                        ? Sixel.BackgroundColor
                        : Sixel.TerminalBackgroundColor);
        }
        return color;
    }

    /// <summary>
    /// Convert to <see cref="Color"/>
    /// </summary>
    public readonly Color ToColor()
    {
#if IMAGESHARP4
        return Color.FromScaledVector(ToScaledVector4());
#else
        var rgba = ToRgba32();
        return Color.FromRgba(rgba.R, rgba.G, rgba.B, rgba.A);
#endif
    }

#if IMAGESHARP4
    /// <summary>
    /// Convert to <see cref="Vector4"/>
    /// </summary>
    public readonly Vector4 ToScaledVector4()
    {
        return new((float)(R / 100.0), (float)(G / 100.0), (float)(B / 100.0), (float)(A / 100.0));
    }
#endif

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

    /// <summary>
    /// Blend <paramref name="background"/> color and make alpha value to 100%
    /// </summary>
    /// <param name="background">Background color</param>
    public void Blend(Color background)
    {
        if (background == Color.Transparent)
            return;

        if (A == 100)
            return;

        var bg = FromRgba32(background.ToPixel<Rgba32>());
        if (A == 0)
        {
            (R, G, B, A) = (bg.R, bg.G, bg.B, bg.A);
            return;
        }

        double alpha = A / 100.0;
        (R, G, B, A) = (
            (byte)((R * alpha) + ((1.0 - alpha) * bg.R)),
            (byte)((G * alpha) + ((1.0 - alpha) * bg.G)),
            (byte)((B * alpha) + ((1.0 - alpha) * bg.B)),
            100
        );
    }
}
