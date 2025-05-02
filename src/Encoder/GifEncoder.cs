using SixLabors.ImageSharp;
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

    protected override int GetFrameDelay(ImageFrame<Rgba32> frame)
    {
        return frame.Metadata.GetGifMetadata().FrameDelay * 1000 / 100;
    }
}
