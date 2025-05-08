using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Tiff;
using SixLabors.ImageSharp.PixelFormats;

namespace SixPix.Encoder;

public class TiffEncoder : SixelEncoder
{
    public TiffEncoder(Image<Rgba32> img) : base(img, "TIFF")
    {
        Metadata = img.Metadata.GetTiffMetadata();
    }

    public TiffMetadata Metadata { get; }

    public override bool ReverseTransparencyOnAnimate => false;

    public override int GetFrameDelay(int frameIndex)
    {
        var delay = FrameDelays[Math.Min(frameIndex, FrameDelays.Length - 1)];
        return delay < 0 ? 500 : delay;
    }
}
