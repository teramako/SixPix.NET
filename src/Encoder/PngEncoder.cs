using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;

namespace SixPix.Encoder;

public class PngEncoder : SixelEncoder
{
    public PngEncoder(Image<Rgba32> img) : base(img, "PNG")
    {
        Metadata = img.Metadata.GetPngMetadata();
        if (Metadata.ColorType == PngColorType.Palette)
            TransparentColor = Metadata.TransparentColor;
    }

    public PngMetadata Metadata { get; }

    public override uint RepeatCount => Metadata.RepeatCount;

    protected override int GetFrameDelay(ImageFrame<Rgba32> frame)
    {
        return (int)(frame.Metadata.GetPngMetadata().FrameDelay.ToDouble() * 1000);
    }
}
