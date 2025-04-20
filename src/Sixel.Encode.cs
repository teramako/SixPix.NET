#if IMAGESHARP4 // ImageSharp v4.0 adds support for CUR and ICO files
using System.Numerics;
#endif
using System.Text;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace SixPix;

public partial class Sixel
{
    public enum Transparency
    {
        Default,    // Standard transparency (palette or alpha channel)
        TopLeft,    // Make the color found at the top left corner (0, 0) transparent
        Background, // Make the background color transparent (for some GIF or WebP images)
        None        // No transparency
    }
    public const char ESC = '\x1b';
    public const string OpaqueStart = "P7;0;q\"1;1";
    public const string TranspStart = "P7;1;q\"1;1";
    public const string End = "\\";

    private const byte specialChNr = 0x6d;
    private const byte specialChCr = 0x64;

    /// <summary>
    /// Encode Image stream to Sixel string
    /// </summary>
    /// <param name="stream">Image stream</param>
    /// <param name="size">Image size (for scaling), or null</param>
    /// <param name="transp">Transparency enum</param>
    /// <param name="frame"><see cref="SixLabors.ImageSharp.ImageFrame"/> index, 0=first/only frame, -1=choose best</param>
    /// <returns>Sixel string</returns>
    public static ReadOnlySpan<char> Encode(Stream stream, Size? size = null, Transparency transp = Transparency.Default, int frame = -1)
    {
        DecoderOptions opt = new();
        if (size?.Width > 0 && size?.Height > 0)
        {
            opt = new()
            {
                TargetSize = new(size?.Width ?? 1, size?.Height ?? 1),
            };
        }
        using var img = Image.Load<Rgba32>(opt, stream);
        return Encode(img, size, transp, frame);
    }
    /// <summary>
    /// Encode <see cref="SixLabors.ImageSharp.Image"/> to Sixel string
    /// </summary>
    /// <param name="img">Image data</param>
    /// <inheritdoc cref="Encode"/>
    public static ReadOnlySpan<char> Encode(Image<Rgba32> img, Size? size = null, Transparency transp = Transparency.Default, int frame = -1)
    {
        int canvasWidth = -1, canvasHeight = -1;
        if (size?.Width < 1 && size?.Height > 0)
        {
            // Keep aspect ratio
            canvasHeight = size?.Height ?? 1;
            canvasWidth = canvasHeight * img.Width / img.Height;
        }
        else if (size?.Height < 1 && size?.Width > 0)
        {
            // Keep aspect ratio
            canvasWidth = size?.Width ?? 1;
            canvasHeight = canvasWidth * img.Height / img.Width;
        }
        else if (size?.Height > 0 && size?.Width > 0)
        {
            canvasWidth = size?.Width ?? 1;
            canvasHeight = size?.Height ?? 1;
        }

        // TODO: Use maximum size based on size of terminal window?
        if (canvasWidth < 1)
            canvasWidth = img.Width;
        if (canvasHeight < 1)
            canvasHeight = img.Height;

        var meta = img.Metadata;
        Color? bg = null, tc = null;
        var format = meta.DecodedImageFormat?.Name;
        int frameCount = img.Frames.Count;

        // Detect images with backgrounds that might be made transparent
        switch (format?.ToUpperInvariant())
        {
            case "GIF":
                var gifMeta = meta.GetGifMetadata();
                bg = gifMeta.GlobalColorTable?.Span[gifMeta.BackgroundColorIndex];
                break;
            case "PNG":
                var pngMeta = meta.GetPngMetadata();
                if (pngMeta.ColorType == PngColorType.Palette)
                    tc = pngMeta.TransparentColor;
                break;
            case "WEBP":
                bg = meta.GetWebpMetadata().BackgroundColor;
                break;
        }

        // Detect images with multiple frames
        switch (format?.ToUpperInvariant())
        {
#if IMAGESHARP4 // ImageSharp v4.0 adds support for CUR and ICO files
            case "CUR":
            case "ICO":
                if (frameCount > 1)
                {
                    if (frame > -1)
                    {
                        if (frame < frameCount)
                            img = img.Frames.ExportFrame(frame);
                        else
                            img = img.Frames.ExportFrame(frame % frameCount);
                    }
                    else
                        img = img.Frames.ExportFrame(GetBestFrame(img, new(canvasWidth, canvasHeight)));
                }
                break;
#endif
            case "GIF":
            case "PNG":  // APNG animations supported
            case "TIFF": // Can contain multiple pages
            case "WEBP":
                if (frameCount > 1 && frame > -1)
                    if (frame < frameCount)
                        img = img.Frames.ExportFrame(frame);
                    else
                        img = img.Frames.ExportFrame(frame % frameCount);
                break;
        }

        DebugPrint($"Width: {canvasWidth}, Height: {canvasHeight}, (bpp={img.PixelType.BitsPerPixel})", lf: true);
        DebugPrint($"Num ImageFrames: {frameCount}", lf: true);
        if (canvasWidth > 1 && canvasHeight > 1 && img.Width != canvasWidth && img.Height != canvasHeight)
        {
            img.Mutate(x => x.Resize(canvasWidth, canvasHeight));
        }

        // 減色処理
        // Color Reduction
        img.Mutate(x =>
        {
            x.Quantize(KnownQuantizers.Wu);
        });

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
        
        var sixelStart = TranspStart;
        if (transp == Transparency.None)
            sixelStart = OpaqueStart;
        sb.Append(ESC + sixelStart)
          .Append($";{canvasWidth};{canvasHeight}".AsSpan());

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
            else if (transp == Transparency.Background && bg is not null && bg == Color.FromScaledVector(new Vector4(rgb.R, rgb.G, rgb.B, 0)))
                (r, g, b) = (0, 0, 0);
#else
            else if (tc is not null && tc == Color.FromRgb(rgb.R, rgb.G, rgb.B))
                (r, g, b) = (0, 0, 0);
            else if (transp == Transparency.Background && bg is not null && bg == Color.FromRgb(rgb.R, rgb.G, rgb.B))
                (r, g, b) = (0, 0, 0);
#endif
            else
                (r, g, b) = (rgb.R * 100 / 0xFF, rgb.G * 100 / 0xFF, rgb.B * 100 / 0xFF);

            // DECGCI (#): Graphics Color Introducer
            sb.Append($"#{i};2;{r:d};{g:d};{b:d}".AsSpan());
            DebugPrint($"#{i};2;", ConsoleColor.Red);
            DebugPrint($"{r:d};{g:d};{b:d}", ConsoleColor.Green, true);
        }
        DebugPrint("End Palette", ConsoleColor.DarkGray, true);

        var buffer = new byte[canvasWidth * colorPaletteLength];
        var cset = new bool[colorPaletteLength]; // 表示すべきカラーパレットがあるかのフラグ
                                                 // Flag to indicate whether there is a color palette to display
        var ch0 = specialChNr;
        for (var (z, y) = (0, 0); z < (canvasHeight + 5) / 6; z++, y = z * 6)
        {
            if (z > 0)
            {
                // DECGNL (-): Graphics Next Line
                sb.Append('-');
                DebugPrint("-", lf: true);
            }
            DebugPrint($"[{z}]", ConsoleColor.DarkGray);
            for (var p = 0; p < 6 && y < canvasHeight; p++, y++)
            {
                for (var x = 0; x < canvasWidth; x++)
                {
                    var idx = colorPalette.IndexOf(img[x, y]);
                    if (colorPalette[idx].A == 0)
                        cset[idx] = false;
                    else if (transp == Transparency.TopLeft && idx == 0)
                        cset[idx] = false;
                    else if (transp == Transparency.Background && bg is not null && bg.Equals(colorPalette[idx]))
                        cset[idx] = false;
                    else
                        cset[idx] = true;

                    buffer[(canvasWidth * idx) + x] |= (byte)(1 << p);
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

                sb.Append($"#{n}".AsSpan());
                DebugPrint($"#{n}", ConsoleColor.Red, false);
                var cnt = 0;
                byte ch;
                int bufIndex;
                char sixelChar;
                for (var x = 0; x < canvasWidth; x++)
                {
                    // make sixel character from 6 pixels
                    bufIndex = (canvasWidth * n) + x;
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
                                sb.Append($"!{cnt}".AsSpan()).Append(sixelChar);
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
                            sb.Append($"!{cnt}".AsSpan()).Append(sixelChar);
                            DebugPrint($"!{cnt}{sixelChar}", ConsoleColor.Cyan);
                            break;
                    }
                }
                ch0 = specialChCr;
            }
        }
        sb.Append(ESC + End);
        DebugPrint("End", ConsoleColor.DarkGray, true);
        return sb.ToString();
    }

    /// <summary>
    /// Get Image format string from <see cref="SixLabors.ImageSharp.Metadata.ImageMetadata"/>
    /// </summary>
    /// <param name="stream">Image Stream</param>
    /// <returns>Format name string, e.g. "PNG"</returns>
    public static string GetFormat(Stream stream)
    {
        return GetFormat(Image.Load<Rgba32>(new(), stream));
    }
    /// <param name="img">Image data</param>
    /// <inheritdoc cref="GetFormat"></inheritdoc>
    public static string GetFormat(Image<Rgba32> img)
    {
        return img.Metadata.DecodedImageFormat?.Name ?? "Unknown";
    }

    /// <summary>
    /// Get suggested number of times to repeat animation (GIF, APNG, or WEBP)
    /// </summary>
    /// <param name="stream">Image Stream</param>
    /// <returns>int number of repeats, 0=continuous, -1=not applicable</returns>
    public static int GetRepeatCount(Stream stream)
    {
        return GetRepeatCount(Image.Load<Rgba32>(new(), stream));
    }
    /// <param name="img">Image data</param>
    /// <inheritdoc cref="GetRepeatCount"></inheritdoc>
    public static int GetRepeatCount(Image<Rgba32> img)
    {
        var meta = img.Metadata;
        switch (meta.DecodedImageFormat?.Name.ToUpperInvariant())
        {
            case "GIF":
                return (int?)meta.GetGifMetadata().RepeatCount ?? -1;
            case "PNG":
                return (int?)meta.GetPngMetadata().RepeatCount ?? -1;
            case "WEBP":
                return (int?)meta.GetWebpMetadata().RepeatCount ?? -1;
        }
        return -1;
    }

    /// <summary>
    /// Get number of ImageFrames (GIF, APNG, or WEBP for animation frames; TIFF for multiple pages; CUR, ICO for various sizes)
    /// </summary>
    /// <param name="stream">Image Stream</param>
    /// <returns>int number of ImageFrames</returns>
    public static int GetNumFrames(Stream stream)
    {
        return GetNumFrames(Image.Load<Rgba32>(new(), stream));
    }
    /// <param name="img">SixLabors.ImageSharp.Image data</param>
    /// <inheritdoc cref="GetNumFrames"></inheritdoc>
    public static int GetNumFrames(Image<Rgba32> img)
    {
        return img.Frames.Count;
    }

    private static ReadOnlySpan<Rgba32> GetColorPalette(Image<Rgba32> image)
    {
        Span<Rgba32> rgbData = new Rgba32[image.Width * image.Height];
        image.CopyPixelDataTo(rgbData);
        return new HashSet<Rgba32>(rgbData.ToArray()).ToArray();
    }

#if IMAGESHARP4 // ImageSharp v4.0 adds support for CUR and ICO files
    /// <summary>
    /// Determine best-sized ImageFrame (for CUR and ICO)
    /// </summary>
    /// <param name="stream">Image Stream</param>
    /// <param name="size">Size, null=largest ImageFrame</param>
    /// <returns>int index of best ImageFrame</returns>
    public static int GetBestFrame(Stream stream, Size? size)
    {
        return GetBestFrame(Image.Load<Rgba32>(new(), stream), size);
    }
    /// <param name="img">Image data</param>
    /// <inheritdoc cref="GetBestFrame"></inheritdoc>
    public static int GetBestFrame(Image<Rgba32> img, Size? size)
    {
        size ??= new(-1, -1);
        int? sizeDim;
        int bestFrame = 0, bestDim = 0, maxBpp = 0, i = 0;
        if (size?.Width > size?.Height)
            sizeDim = size?.Width;
        else
            sizeDim = size?.Height;
        foreach (var frame in img.Frames)
        {
            var meta = frame.Metadata.GetIcoMetadata();
            DebugPrint("  " + i + ":" + meta.EncodingWidth + "x" + meta.EncodingHeight + "x" + (int)meta.BmpBitsPerPixel + "b", lf: true);
            if ((int)meta.BmpBitsPerPixel >= maxBpp)
            {
                maxBpp = (int)meta.BmpBitsPerPixel;
                int w = meta.EncodingWidth;
                //int h = meta.EncodingHeight;
                if (w == 0) // oddly, 0 means 256
                    w = 256;
                if ((bestDim <= 0) ||
                    ((sizeDim is null || sizeDim <= 0) && w > bestDim) ||
                    (sizeDim is not null && sizeDim > 0 && w >= sizeDim && w < bestDim) ||
                    (w > bestDim))
                {
                    bestDim = w;
                    bestFrame = i;
                }
            }
            i++;
        }
        DebugPrint("Best ImageFrame: " + bestFrame, lf: true);
        return bestFrame;
    }
#endif
}
