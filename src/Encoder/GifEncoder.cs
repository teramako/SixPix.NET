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

    public override IFrameMetadata GetFrameMetadata(int index)
    {
        var frame = Image.Frames[index];
        return new GifFrameMetadataWrapper(frame.Metadata.GetGifMetadata());
    }
}

internal class GifFrameMetadataWrapper(GifFrameMetadata metadata)
    : IFrameMetadata
{
    public GifFrameMetadata Metadata => metadata;

    public int FrameDelay => metadata.FrameDelay * 1000 / 100;
}
