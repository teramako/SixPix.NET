#if IMAGESHARP4 // ImageSharp v4.0 adds support for CUR and ICO files
using System.Numerics;
#endif
using System.Text;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace SixPix;

public partial class Sixel
{
    const char ESC = '\x1b';
    const string SixelStart = "P7;1;q\"1;1";
    const string SixelEnd = "\\";

    const byte specialChNr = (byte)0x6d;
    const byte specialChCr = (byte)0x64;

    /// <summary>
    /// Encode Image stream to Sixel string
    /// </summary>
    /// <param name="stream">Image stream</param>
    /// <param name="tc">Transparent color (for palette-based PNG images), or null</param>
    /// <param name="bg">Background color (for some GIF or WebP images), or null</param>
    /// <param name="transp_bg">Make the background color transparent (for some GIF or WebP images)</param>
    /// <param name="transp_tl">Make the color found at the top left corner (0, 0) transparent</param>
    /// <returns>Sixel string</returns>
    public static ReadOnlySpan<char> Encode(Stream stream, Color? tc = null, Color? bg = null, bool transp_bg = false, bool transp_tl = false)
    {
        using var img = Image.Load<Rgba32>(stream);
        return Encode(img, tc, bg, transp_bg, transp_tl);
    }
    /// <summary>
    /// Encode Image to Sixel string
    /// </summary>
    /// <param name="img">Image data</param>
    /// <param name="tc">Transparent color (for palette-based PNG images)</param>
    /// <param name="bg">Background color (for some GIF or WebP images)</param>
    /// <param name="transp_bg">Make the background color transparent (for some GIF or WebP images)</param>
    /// <param name="transp_tl">Make the color found at the top left corner (0, 0) transparent</param>
    /// <returns>Sixel string</returns>
    public static ReadOnlySpan<char> Encode(Image<Rgba32> img, Color? tc = null, Color? bg = null, bool transp_bg = false, bool transp_tl = false)
    {
#if IMAGESHARP4 // ImageSharp v4.0 adds support for CUR and ICO files
        if ((img.Metadata.DecodedImageFormat?.Name == "CUR" ||
            img.Metadata.DecodedImageFormat?.Name == "ICO") &&
            img.Frames.Count > 0)
            img = img.Frames.ExportFrame(GetBestIconFrame(img));
#endif

        // 減色処理
        // Color Reduction
        img.Mutate(x => {
            x.Quantize(KnownQuantizers.Wu);
        });
        var width = img.Width;
        var height = img.Height;

        DebugPrint($"Width: {width}, Height: {height}, (bpp={img.PixelType.BitsPerPixel})", lf: true);
        if (tc is not null)
            DebugPrint($"Transparent Palette Color={tc?.ToHex()}", lf: true);
        else if (bg is not null)
            DebugPrint($"Background Color={bg?.ToHex()}", lf: true);
        else
            DebugPrint($"No Background or Transparent palette color found.", lf: true);

        // カラーパレットの構築
        // Building a color palette
        ReadOnlySpan<Rgba32> colorPalette = GetColorPalette(img);

        //
        // https://github.com/mattn/go-sixel/blob/master/sixel.go の丸パクリです！！
        //                                                        It's a complete rip-off!!
        //
        var sb = new StringBuilder();
        // DECSIXEL Introducer(\033P0;0;8q) + DECGRA ("1;1): Set Raster Attributes
        sb.Append(ESC + SixelStart)
          .Append($";{width};{height}");

        DebugPrint($"Palette Start Length={colorPalette.Length}", lf: true);

        int colorPaletteLength = colorPalette.Length;
        for (var i = 0; i < colorPaletteLength; i++)
        {
            var rgb = colorPalette[i];
            int r = 0, g = 0, b = 0;

            if (rgb.A == 0)
                (r, g, b) = (0, 0, 0);
#if IMAGESHARP4 // ImageSharp v4.0
            else if (tc is not null && tc == Color.FromScaledVector(new Vector4(rgb.R, rgb.G, rgb.B, 0)))
                (r, g, b) = (0, 0, 0);
            else if (transp_bg && bg is not null && bg == Color.FromScaledVector(new Vector4(rgb.R, rgb.G, rgb.B, 0)))
                (r, g, b) = (0, 0, 0);
#else
            else if (tc is not null && tc == Color.FromRgb(rgb.R, rgb.G, rgb.B))
                (r, g, b) = (0, 0, 0);
            else if (transp_bg && bg is not null && bg == Color.FromRgb(rgb.R, rgb.G, rgb.B))
                (r, g, b) = (0, 0, 0);
#endif
            else
                (r, g, b) = (rgb.R * 100 / 0xFF, rgb.G * 100 / 0xFF, rgb.B * 100 / 0xFF);

            // DECGCI (#): Graphics Color Introducer
            sb.Append($"#{i};2;{r:d};{g:d};{b:d}");
            DebugPrint($"#{i};2;", ConsoleColor.Red);
            DebugPrint($"{r:d};{g:d};{b:d}", ConsoleColor.Green, true);
        }
        DebugPrint("End Palette", ConsoleColor.DarkGray, true);

        var buffer = new byte[width * colorPaletteLength];
        var cset = new bool[colorPaletteLength]; // 表示すべきカラーパレットがあるかのフラグ
                                                 // Flag to indicate whether there is a color palette to display
        var ch0 = specialChNr;
        for (var (z, y) = (0, 0); z < (height + 5) / 6; z++, y = z * 6)
        {
            if (z > 0) {
                // DECGNL (-): Graphics Next Line
                sb.Append('-');
                DebugPrint("-", lf: true);
            }
            DebugPrint($"[{z}]", ConsoleColor.DarkGray);
            for (var p = 0; p < 6 && y < height; p++, y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var idx = colorPalette.IndexOf(img[x, y]);
                    if (colorPalette[idx].A == 0)
                        cset[idx] = false;
                    else if (transp_tl && idx == 0)
                        cset[idx] = false;
                    else if (transp_bg && bg is not null && bg.Equals(colorPalette[idx]))
                        cset[idx] = false;
                    else
                        cset[idx] = true;

                    buffer[width * idx + x] |= (byte)(1 << p);
                }
            }
            bool first = true;
            for (var n = 0; n < colorPaletteLength; n++)
            {
                if (!cset[n]) continue;

                cset[n] = false;
                if (ch0 == specialChCr && !first)
                {
                    // DECGCR ($): Graphics Carriage Return
                    sb.Append('$');
                    DebugPrint("$");
                }
                first = false;

                sb.Append($"#{n}");
                DebugPrint($"#{n}", ConsoleColor.Red, false);
                var cnt = 0;
                byte ch;
                int bufIndex;
                char sixelChar;
                for (var x = 0; x < width; x++)
                {
                    // make sixel character from 6 pixels
                    bufIndex = width * n + x;
                    ch = buffer[bufIndex];
                    buffer[bufIndex] = 0;
                    if (ch0 < 0x40 && ch != ch0)
                    {
                        sixelChar = (char)(63 + ch0);
                        for (; cnt > 255; cnt -= 255)
                        {
                            sb.Append("!255").Append(sixelChar);
                            DebugPrint($"!255{sixelChar}", ConsoleColor.Yellow);
                        }
                        switch (cnt)
                        {
                            case 1:
                                sb.Append(sixelChar);
                                DebugPrint($"{sixelChar}", ConsoleColor.Yellow);
                                break;
                            case 2:
                                sb.Append([sixelChar, sixelChar]);
                                DebugPrint($"{sixelChar}{sixelChar}", ConsoleColor.Yellow);
                                break;
                            case 3:
                                sb.Append([sixelChar, sixelChar, sixelChar]);
                                DebugPrint($"{sixelChar}{sixelChar}{sixelChar}", ConsoleColor.Yellow);
                                break;
                            case > 0:
                                sb.Append($"!{cnt}").Append(sixelChar);
                                DebugPrint($"!{cnt}{sixelChar}", ConsoleColor.Yellow);
                                break;
                        }
                        cnt = 0;
                    }
                    ch0 = ch;
                    cnt++;
                }
                if (ch0 != 0)
                {
                    sixelChar = (char)(63 + ch0);
                    for (; cnt > 255; cnt -= 255)
                    {
                        sb.Append("!255").Append(sixelChar);
                        DebugPrint($"!255{sixelChar}", ConsoleColor.Cyan);
                    }
                    switch (cnt)
                    {
                        case 1:
                            sb.Append(sixelChar);
                            DebugPrint($"{sixelChar}", ConsoleColor.Cyan);
                            break;
                        case 2:
                            sb.Append([sixelChar, sixelChar]);
                            DebugPrint($"{sixelChar}{sixelChar}", ConsoleColor.Cyan);
                            break;
                        case 3:
                            sb.Append([sixelChar, sixelChar, sixelChar]);
                            DebugPrint($"{sixelChar}{sixelChar}{sixelChar}", ConsoleColor.Cyan);
                            break;
                        case > 0:
                            sb.Append($"!{cnt}").Append(sixelChar);
                            DebugPrint($"!{cnt}{sixelChar}", ConsoleColor.Cyan);
                            break;
                    }
                }
                ch0 = specialChCr;
            }
        }
        sb.Append(ESC + SixelEnd);
        DebugPrint("End", ConsoleColor.DarkGray, true);
        return sb.ToString();
    }

    private static ReadOnlySpan<Rgba32> GetColorPalette(Image<Rgba32> image)
    {
        Span<Rgba32> rgbData = new Rgba32[image.Width * image.Height];
        image.CopyPixelDataTo(rgbData);
        return new HashSet<Rgba32>(rgbData.ToArray()).ToArray();
    }

#if IMAGESHARP4 // ImageSharp v4.0 adds support for CUR and ICO files
    static int GetBestIconFrame(Image icon)
    {
        int bestFrame = 0, maxWidth = 0, maxBpp = 0, i = 0;
        DebugPrint(icon.Frames.Count + " ImageFrames:", lf: true);
        foreach (var frame in icon.Frames)
        {
            var meta = frame.Metadata.GetIcoMetadata();
            DebugPrint("  " + i + ":" + meta.EncodingWidth + "x" + meta.EncodingHeight + "x" + (int)meta.BmpBitsPerPixel + "b", lf: true);
            if ((int)meta.BmpBitsPerPixel >= maxBpp)
            {
                maxBpp = (int)meta.BmpBitsPerPixel;
                if (meta.EncodingWidth == 0) // oddly, 0 means 256
                {
                    maxWidth = 256;
                    bestFrame = i;
                }
                else if (meta.EncodingWidth > maxWidth)
                {
                    maxWidth = meta.EncodingWidth;
                    bestFrame = i;
                }
            }
            i++;
        }
        return bestFrame;
    }
#endif
}
