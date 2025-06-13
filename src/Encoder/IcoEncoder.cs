#if IMAGESHARP4 // ImageSharp v4.0 adds support for CUR and ICO files
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Ico;
using SixLabors.ImageSharp.PixelFormats;

namespace SixPix.Encoder;

public class IcoEncoder : SixelEncoder
{
    public IcoEncoder(Image<Rgba32> img) : base(img, "ICO")
    {
        Metadata = img.Metadata.GetIcoMetadata();
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

    public IcoMetadata Metadata { get; }

    public override bool ReverseTransparencyOnAnimate => false;

    /// <summary>
    /// Encode a <see cref="ImageFrame"/> into a Sixel string.
    /// The image frame is choosed automaticaly. (typically the root frame)j
    /// </summary>
    public override string Encode()
    {
        return EncodeFrame(GetBestFrame());
    }

    protected override string EncodeFrameInternal(ImageFrame<Rgba32> frame)
    {
        // Quantize the image if not already done
        // and get the color palette for the frame
        if (!Quantized)
            Quantize();
        // Get width and height of the frame metadata
        // The ICO format supports images up to 256 x 256 pixels
        var metadata = frame.Metadata.GetIcoMetadata();
        var size = new Size(metadata.EncodingWidth is null or 0 ? 256 : (int)metadata.EncodingWidth,
                            metadata.EncodingHeight is null or 0 ? 256 : (int)metadata.EncodingHeight);
        return Sixel.EncodeFrame(frame,
                                 GetColorPalette(frame),
                                 size,
                                 TransparencyMode,
                                 TransparentColor,
                                 BackgroundColor);
    }

    public override int GetFrameDelay(int frameIndex)
    {
        var delay = FrameDelays[Math.Min(frameIndex, FrameDelays.Length - 1)];
        return delay < 0 ? 500 : delay;
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
