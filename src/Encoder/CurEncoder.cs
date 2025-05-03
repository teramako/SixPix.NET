#if IMAGESHARP4 // ImageSharp v4.0 adds support for CUR and ICO files
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Cur;
using SixLabors.ImageSharp.PixelFormats;

namespace SixPix.Encoder;

public class CurEncoder : SixelEncoder
{
    public CurEncoder(Image<Rgba32> img) : base(img, "CUR")
    {
        Metadata = img.Metadata.GetCurMetadata();
        // Ensure the image size is within the ICO/CUR limits
        // Set the size of the image to 256 x 256 if the image is too large
        if (img.Width > 256)
        {
            Resize(256, -1);
        }
        else if (img.Height > 256)
        {
            Resize(-1, 256);
        }
    }

    public CurMetadata Metadata { get; }

    public override bool CanAnimate => false;

    /// <summary>
    /// Encode a <see cref="ImageFrame"/> into a Sixel string.
    /// The image frame is choosed automaticaly. (typically the root frame)j
    /// </summary>
    public override string Encode()
    {
        return EncodeFrame(GetBestFrame());
    }

    public override string EncodeFrame(ImageFrame<Rgba32> frame)
    {
        // Get width and height of the frame metadata
        // The ICO format supports images up to 256 x 256 pixels
        var metadata = frame.Metadata.GetCurMetadata();
        var size = new Size(metadata.EncodingWidth == 0 ? 256 : metadata.EncodingWidth,
                            metadata.EncodingHeight == 0 ? 256 : metadata.EncodingHeight);
        return EncodeFrame(frame, size);
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
