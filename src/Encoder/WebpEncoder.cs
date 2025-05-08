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

    public override int GetFrameDelay(int frameIndex)
    {
        var delay = FrameDelays[Math.Min(frameIndex, FrameDelays.Length - 1)];
        if (delay < 0)
        {
            var frame = Image.Frames[frameIndex];
            return (int)frame.Metadata.GetWebpMetadata().FrameDelay;
        }
        return delay;
    }
}
