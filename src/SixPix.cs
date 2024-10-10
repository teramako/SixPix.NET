using System.Diagnostics;
using System.Text;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors.Quantization;

namespace SixPix;

public class Sixel
{
    const char ESC = (char)0x1b;
    static readonly char[] SixelStart = [ESC, 'P', 'q'];
    static readonly char[] SixcelEnd = [ESC, '\\'];

    const byte specialChNr = (byte)0x6d;
    const byte specialChCr = (byte)0x64;

    const int MAX_PALLETE_LENGTH = 255;

    [Conditional("DEBUG")]
    static void DebugPrint(string msg, ConsoleColor fg = ConsoleColor.Magenta)
    {
        var currentFg = Console.ForegroundColor;
        Console.ForegroundColor = fg;
        Console.WriteLine(msg);
        Console.ForegroundColor = currentFg;
    }

    /// <summary>
    /// Encode Image stream to Sixel string
    /// </summary>
    /// <param name="stream">Image Stream</param>
    /// <returns>Sxiel string</returns>
    public static string Encode(Stream stream)
    {
        var img = Image.Load<Rgb24>(stream);
        // 減色処理
        img.Mutate(x => {
            x.Quantize(KnownQuantizers.Octree);
        });
        var width = img.Width;
        var height = img.Height;

        DebugPrint($"Width: {width}, Height: {height}, (bpp={img.PixelType.BitsPerPixel}) {img.PixelType.AlphaRepresentation} {img}");

        // カラーパレットの構築
        // XXX: もっと良い方法がありそう
        Rgb24[] colorPalette;
        using (var quantizer = KnownQuantizers.Octree.CreatePixelSpecificQuantizer<Rgb24>(img.Configuration))
        {
            quantizer.BuildPalette(new DefaultPixelSamplingStrategy(), img);
            colorPalette = new HashSet<Rgb24>(quantizer.Palette.ToArray()).ToArray();
        }

        //
        // https://github.com/mattn/go-sixel/blob/master/sixel.go の丸パクリです！！
        //
        var sb = new StringBuilder();
        sb.Append(SixelStart);
        // DECSIXEL Introducer(\033P0;0;8q) + DECGRA ("1;1): Set Raster Attributes
        sb.Append(new char[] { ESC, 'P', ';', '0', ';', '8', 'q', '"', '1', ';', '1' });
        sb.Append($"{width};{height}");

        DebugPrint($"Palette Length: {colorPalette.Length}");
        for (var i = 0; i < colorPalette.Length; i++)
        {
            var rgb = colorPalette[i];
            var (r, g, b) = (rgb.R * 100 / 0xFF, rgb.G * 100 / 0xFF, rgb.B * 100 / 0xFF);
            // DECGCI (#): Graphics Color Introducer
            sb.Append($"#{i+1};2;{r:d};{g:d};{b:d}");
        }

        var buffer = new byte[width * MAX_PALLETE_LENGTH];
        var cset = new bool[MAX_PALLETE_LENGTH];
        var ch0 = specialChNr;
        for (var z = 0; z < (height + 5) / 6; z++)
        {
            if (z > 0) {
                // DECGNL (-): Graphics Next Line
                sb.Append('-');
                DebugPrint($"[z={z}]: Next Line");
            }
            for (var p = 0; p < 6; p++)
            {
                var y = z * 6 + p;
                for (var x = 0; x < width && y < height; x++)
                {
                    var rgb = img[x, y];
                    var idx = Array.IndexOf(colorPalette, rgb);
                    cset[idx] = true;
                    buffer[width * idx + x] |= (byte)(1 << p);
                }
            }
            for (var n = 0; n < MAX_PALLETE_LENGTH; n++)
            {
                if (!cset[n]) continue;

                cset[n] = true;
                cset[n] = false;
                if (ch0 == specialChCr)
                {
                    // DECGCR ($): Graphics Carriage Return
                    sb.Append('$');
                    DebugPrint($"[z={z},n={n}]: Carrige Return (Rewrite Same Line)");
                }

                sb.Append($"#{n+1}");
                var cnt = 0;
                for (var x = 0; x < width; x++)
                {
                    // make sixel character from 6 pixels
                    var bufIndex = width * n + x;
                    var ch = buffer[bufIndex];
                    buffer[bufIndex] = 0;
                    if (ch0 < 0x40 && ch != ch0)
                    {
                        var sixelChar = (char)(63 + ch0);
                        for (; cnt > 255; cnt -= 255)
                        {
                            sb.Append("!255").Append(sixelChar);
                        }
                        if (cnt == 1)
                        {
                            sb.Append(sixelChar);
                        }
                        else if (cnt == 2)
                        {
                            sb.Append([sixelChar, sixelChar]);
                        }
                        else if (cnt == 3)
                        {
                            sb.Append([sixelChar, sixelChar, sixelChar]);
                        }
                        else
                        {
                            sb.Append($"!{cnt}").Append(sixelChar);
                        }
                        cnt = 0;
                    }
                    ch0 = ch;
                    cnt++;
                }
                if (ch0 != 0)
                {
                    var sixelChar = (char)(63 + ch0);
                    for (; cnt > 255; cnt -= 255)
                    {
                        sb.Append("!255").Append(sixelChar);
                    }
                    if (cnt == 1)
                    {
                        sb.Append(sixelChar);
                    }
                    else if (cnt == 2)
                    {
                        sb.Append([sixelChar, sixelChar]);
                    }
                    else if (cnt == 3)
                    {
                        sb.Append([sixelChar, sixelChar, sixelChar]);
                    }
                    else
                    {
                        sb.Append($"!{cnt}").Append(sixelChar);
                    }
                    cnt = 0;
                }
                ch0 = specialChCr;
            }
        }
        sb.Append(SixcelEnd);
        return sb.ToString();
    }
}
