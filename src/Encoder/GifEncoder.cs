using SixLabors.ImageSharp;
#if IMAGESHARP4
using SixLabors.ImageSharp.Formats;
#endif
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.PixelFormats;

namespace SixPix.Encoder;

public class GifEncoder : SixelEncoder
{
    public GifEncoder(Image<Rgba32> img) : base(img, "GIF")
    {
        Metadata = img.Metadata.GetGifMetadata();
        BackgroundColor = Metadata.GlobalColorTable?.Span[Metadata.BackgroundColorIndex];
        // Gif format is already 256 colors, don't need to quantize
        Quantized = true;
    }

    public GifMetadata Metadata { get; }

    public override uint RepeatCount => Metadata.RepeatCount;

    private SixelColor[] globalColorPalette = [];
    public ReadOnlySpan<SixelColor> GlobalColorPalette => globalColorPalette;

    /// <summary>
    /// Create color palette from Global or Local Color Table of Gif
    /// If failed, fall back to create from the <paramref name="frame"/>
    /// </summary>
    /// <param name="frame"></param>
    protected override ReadOnlySpan<SixelColor> GetColorPalette(ImageFrame<Rgba32> frame)
    {
        var meta = frame.Metadata.GetGifMetadata();
        ReadOnlyMemory<Color>? colorTable = null;

        bool isGlobalColorTable = false;
        int gbIndex = -1;
#if IMAGESHARP4
        if (meta.ColorTableMode == FrameColorTableMode.Global)
#else
        if (meta.ColorTableMode == GifColorTableMode.Global)
#endif
        {
            if (globalColorPalette.Length != 0)
            {
                return globalColorPalette;
            }
            isGlobalColorTable = true;
            colorTable = Metadata.GlobalColorTable;
            gbIndex = Metadata.BackgroundColorIndex;
        }
#if IMAGESHARP4
        else if (meta.ColorTableMode == FrameColorTableMode.Local)
#else
        else if (meta.ColorTableMode == GifColorTableMode.Local)
#endif
        {
            colorTable = meta.LocalColorTable;
            gbIndex = meta.TransparencyIndex;
        }

        if (colorTable is not null)
        {
            var colorSpan = colorTable.Value.Span;
            Color? bgColor = gbIndex >= 0 && gbIndex < colorSpan.Length ? colorSpan[gbIndex] : null;
            var colorPalette = new SixelColor[colorSpan.Length];
            for (var i = 0; i < colorTable.Value.Span.Length; i++)
            {
                colorPalette[i] = SixelColor.FromColor(colorSpan[i], TransparencyMode, null, bgColor);
            }
            if (isGlobalColorTable)
            {
                // Cache the global color palette
                globalColorPalette = colorPalette;
            }
            return colorPalette;
        }
        // Fallback to base class method.
        // This will create a color palette from the image frame
        return base.GetColorPalette(frame);
    }

    /// <remarks>
    /// Gif format is already 256 colors, skip quantization.
    /// And detect transparency color from local color table.
    /// </remarks>
    /// <inheritdoc cref="SixelEncoder.EncodeFrameInternal(ImageFrame{Rgba32})"/>
    protected override string EncodeFrameInternal(ImageFrame<Rgba32> frame)
    {
        var meta = frame.Metadata.GetGifMetadata();
        var bgColor = meta.LocalColorTable?.Span[meta.TransparencyIndex] ?? BackgroundColor;
        return Sixel.EncodeFrame(frame,
                                 GetColorPalette(frame),
                                 CanvasSize,
                                 TransparencyMode,
                                 TransparentColor,
                                 bgColor);
    }

    protected override int GetFrameDelay(ImageFrame<Rgba32> frame)
    {
        return frame.Metadata.GetGifMetadata().FrameDelay * 1000 / 100;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            // Dispose of the global color palette if it was created
            if (globalColorPalette.Length != 0)
            {
                globalColorPalette = [];
            }
        }
        base.Dispose(disposing);
    }
}
