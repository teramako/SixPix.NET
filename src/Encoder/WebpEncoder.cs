using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.PixelFormats;

namespace SixPix.Encoder;

public class WebpEncoder : SixelEncoder
{
    public WebpEncoder(Image<Rgba32> img) : base(img, "WEBP")
    {
        Metadata = img.Metadata.GetWebpMetadata();
        BackgroundColor = Metadata.BackgroundColor;
    }

    public WebpMetadata Metadata { get; }

    public override uint RepeatCount => Metadata.RepeatCount;

    protected override int GetFrameDelay(ImageFrame<Rgba32> frame)
    {
        return (int)frame.Metadata.GetWebpMetadata().FrameDelay;
    }
}
