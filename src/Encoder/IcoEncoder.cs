#if IMAGESHARP4 // ImageSharp v4.0 adds support for CUR and ICO files
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Ico;
using SixLabors.ImageSharp.PixelFormats;

namespace SixPix.Encoder;

internal class IcoEncoder : SixelEncoder
{
    public IcoEncoder(Image<Rgba32> img) : base(img, "ICO")
    {
        Metadata = img.Metadata.GetIcoMetadata();
    }

    public IcoMetadata Metadata { get; }

    public override bool CanAnimate => false;

    /// <summary>
    /// Encode a <see cref="ImageFrame"/> into a Sixel string.
    /// The image frame is choosed automaticaly. (typically the root frame)j
    /// </summary>
    public override string Encode()
    {
        return EncodeFrame(GetBestFrame());
    }

    /// <summary>
    /// Determine best-sized ImageFrame (for CUR and ICO)
    /// </summary>
    /// <param name="size">Size, null=largest ImageFrame</param>
    /// <returns>int index of best ImageFrame</returns>
    public int GetBestFrame(Size? size = null)
    {
        return Sixel.GetBestFrame(Image, size);
    }
}
#endif
