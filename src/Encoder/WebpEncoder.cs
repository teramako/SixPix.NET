using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.PixelFormats;

namespace SixPix.Encoder;

internal class WebpEncoder : SixelEncoder
{
    public WebpEncoder(Image<Rgba32> img) : base(img, "WEBP")
    {
        Metadata = img.Metadata.GetWebpMetadata();
        BackgroundColor = Metadata.BackgroundColor;
    }

    public WebpMetadata Metadata { get; }

    public override uint RepeatCount => Metadata.RepeatCount;

    public override IFrameMetadata GetFrameMetadata(int index)
    {
        var frame = Image.Frames[index];
        return new WebpFrameMetadataWrapper(frame.Metadata.GetWebpMetadata());
    }
}

internal class WebpFrameMetadataWrapper(WebpFrameMetadata metadata)
    : IFrameMetadata
{
    public WebpFrameMetadata Metadata => metadata;

    public int FrameDelay => (int)metadata.FrameDelay;
}
