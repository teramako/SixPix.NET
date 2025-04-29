using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;

namespace SixPix.Encoder;

internal class PngEncoder : SixelEncoder
{
    public PngEncoder(Image<Rgba32> img) : base(img, "PNG")
    {
        Metadata = img.Metadata.GetPngMetadata();
        if (Metadata.ColorType == PngColorType.Palette)
            TransparentColor = Metadata.TransparentColor;
    }

    public PngMetadata Metadata { get; }

    public override uint RepeatCount => Metadata.RepeatCount;

    public override IFrameMetadata GetFrameMetadata(int index)
    {
        var frame = Image.Frames[index];
        return new PngFrameMetadataWrapper(frame.Metadata.GetPngMetadata());
    }
}

internal class PngFrameMetadataWrapper(PngFrameMetadata metadata)
    : IFrameMetadata
{
    public PngFrameMetadata Metadata => metadata;

    public int FrameDelay => (int)(metadata.FrameDelay.ToDouble() * 1000);
}
