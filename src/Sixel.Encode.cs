using System.Text;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixPix.Encoder;

namespace SixPix;

public enum Transparency
{
    Default,    // Standard transparency (palette or alpha channel)
    TopLeft,    // Make the color found at the top left corner (0, 0) transparent
    Background, // Make the background color transparent (for some GIF or WebP images)
    None        // No transparency
}

public static partial class Sixel
{
    public const char ESC = '\x1b';
    public const string OpaqueStart = "P7;0;q\"1;1";
    public const string TranspStart = "P7;1;q\"1;1";
    public const string End = "\\";

    private const byte specialChNr = 0x6d;
    private const byte specialChCr = 0x64;

    /// <summary>
    /// Create an encoder instance to convert <paramref name="image"/> to Sixel string
    /// </summary>
    /// <param name="image"></param>
    public static SixelEncoder CreateEncoder(Image<Rgba32> image)
    {
        var format = image.Metadata.DecodedImageFormat?.Name.ToUpperInvariant();
        return format switch
        {
            "GIF" => new GifEncoder(image),
            "PNG" => new PngEncoder(image),
            "WEBP" => new WebpEncoder(image),
            "TIFF" => new TiffEncoder(image),
#if IMAGESHARP4 // ImageSharp v4.0 adds support for CUR and ICO files
            "ICO" => new IcoEncoder(image),
            "CUR" => new CurEncoder(image),
#endif
            _ => new SixelEncoder(image, format),
        };
    }
    /// <summary>
    /// Create an encoder instance to convert the file <paramref name="path"/> to a Sixel string
    /// </summary>
    /// <param name="path">Image file path</param>
    public static SixelEncoder CreateEncoder(string path)
    {
        return File.Exists(path)
            ? CreateEncoder(Image.Load<Rgba32>(path))
            : throw new FileNotFoundException("File not found", path);
    }
    /// <summary>
    /// Create an encoder instance to convert the file <paramref name="path"/> to a Sixel string
    /// </summary>
    /// <param name="stream">a stream for image</param>
    public static SixelEncoder CreateEncoder(Stream stream)
    {
        return CreateEncoder(Image.Load<Rgba32>(stream));
    }

    /// <summary>
    /// Encode Image stream to Sixel string
    /// </summary>
    /// <param name="stream">Image stream</param>
    /// <param name="size">Image size (for scaling), or null</param>
    /// <param name="transp">Transparency enum</param>
    /// <param name="frame"><see cref="SixLabors.ImageSharp.ImageFrame"/> index, 0=first/only frame, -1=choose best</param>
    /// <returns>Sixel string</returns>
    public static ReadOnlySpan<char> Encode(Stream stream,
                                            Size? size = null,
                                            Transparency transp = Transparency.Default,
                                            int frame = -1)
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
    public static ReadOnlySpan<char> Encode(Image<Rgba32> img,
                                            Size? size = null,
                                            Transparency transp = Transparency.Default,
                                            int frame = -1)
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
        var format = meta.DecodedImageFormat?.Name.ToUpperInvariant();
        int frameCount = img.Frames.Count;

        // Detect images with backgrounds that might be made transparent
        switch (format)
        {
            case "GIF":
                var gifMeta = meta.GetGifMetadata();
                bg = gifMeta.GlobalColorTable?.Span[gifMeta.BackgroundColorIndex];
                break;
            case "PNG":
                var pngMeta = meta.GetPngMetadata();
                if (pngMeta.ColorType == SixLabors.ImageSharp.Formats.Png.PngColorType.Palette)
                    tc = pngMeta.TransparentColor;
                break;
            case "WEBP":
                bg = meta.GetWebpMetadata().BackgroundColor;
                break;
        }

        // Detect images with multiple frames
        switch (format)
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

        Color termbg = BackgroundColor;

        var imageFrame = img.Frames.RootFrame;

        // Building a color palette
        ReadOnlySpan<SixelColor> colorPalette = GetColorPalette(imageFrame, transp, tc, bg);
        Size frameSize = new(canvasWidth, canvasHeight);

        return EncodeFrame(imageFrame, colorPalette, frameSize, transp, tc, bg, termbg);
    }

    /// <summary>
    /// Encode <see cref="ImageFrame"/> to Sixel string
    /// </summary>
    /// <param name="frame">a frame part of Image data</param>
    /// <param name="colorPalette">Color palette for Sixel</param>
    /// <param name="frameSize">size of the frame</param>
    /// <param name="tc">Transparent <see cref="Color"/> set for the image</param>
    /// <param name="bg">Background <see cref="Color"/> set for the image</param>
    /// <inheritdoc cref="Encode(Image{Rgba32}, Size?, Transparency, int)"/>
    public static string EncodeFrame(ImageFrame<Rgba32> frame,
                                     ReadOnlySpan<SixelColor> colorPalette,
                                     Size frameSize,
                                     Transparency transp = Transparency.Default,
                                     Color? tc = null,
                                     Color? bg = null,
                                     Color? termBg = null)
    {
        int canvasWidth = frameSize.Width;
        int canvasHeight = frameSize.Height;

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

        DebugPrint($"Palette Start Length={colorPalette.Length}", ConsoleColor.DarkGray, true);
        int colorPaletteLength = colorPalette.Length;
        for (var i = 0; i < colorPaletteLength; i++)
        {
            // DECGCI (#): Graphics Color Introducer
            var colorValue = colorPalette[i].ToColorPalette();
            sb.Append($"#{i};2;{colorValue}".AsSpan());
            DebugPrint($"#{i};2;", ConsoleColor.Red);
            DebugPrint(colorValue, ConsoleColor.Green, true);
        }
        DebugPrint("End Palette", ConsoleColor.DarkGray, true);

        var buffer = new byte[canvasWidth * colorPaletteLength];
        // Flag to indicate whether there is a color palette to display
        var cset = new bool[colorPaletteLength];
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
                    var rgba = frame[x, y];
                    var sixelColor = SixelColor.FromRgba32(rgba, transp, tc, bg);
                    if (sixelColor.A is > 0 and < 100 && termBg is not null)
                    {
                        // Blend the background color to create opaque color
                        sixelColor.Blend(termBg.Value);
                    }
                    var idx = colorPalette.IndexOf(sixelColor);
                    if (idx < 0)
                        continue;
                    if (colorPalette[idx].A == 0)
                        cset[idx] = transp == Transparency.None;
                    else if (transp == Transparency.TopLeft && rgba.Equals(frame[0, 0]))
                        cset[idx] = false;
                    else if (transp == Transparency.Background && bg is not null && bg.Equals(rgba))
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

    /// <summary>
    /// Build color palette for Sixel
    /// </summary>
    public static SixelColor[] GetColorPalette(ImageFrame<Rgba32> frame,
                                               Transparency transp = Transparency.Default,
                                               Color? tc = null,
                                               Color? bg = null,
                                               Color? termbg = null)
    {
        var palette = new HashSet<SixelColor>();
        frame.ProcessPixelRows(accessor =>
        {
            var pixcelHash = new HashSet<Rgba32>();
            for (int y = 0; y < accessor.Height; y++)
            {
                Span<Rgba32> row = accessor.GetRowSpan(y);
                for (int x = 0; x < row.Length; x++)
                {
                    if (pixcelHash.Add(row[x]))
                    {
                        var c = SixelColor.FromRgba32(row[x], transp, tc, bg);
                        if (c.A is > 0 and < 100 && termbg is not null)
                            c.Blend(termbg.Value);
                        palette.Add(c);
                    }
                }
            }
        });
        return [.. palette];
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
