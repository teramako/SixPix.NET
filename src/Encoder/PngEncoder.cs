using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;

namespace SixPix.Encoder;

public class PngEncoder : SixelEncoder
{
    public PngEncoder(Image<Rgba32> img) : base(img, "PNG")
    {
        Metadata = img.Metadata.GetPngMetadata();
        switch (Metadata.ColorType)
        {
            case PngColorType.Palette:
                TransparentColor = Metadata.TransparentColor?.ToPixel<Rgba32>();
                Quantized = true;
                break;
            case PngColorType.Rgb:
                Quantized = true;
                break;
            case PngColorType.Grayscale:
                Quantized = true;
                break;
        }
    }

    public PngMetadata Metadata { get; }

    public override uint RepeatCount => Metadata.RepeatCount;

    public override int GetFrameDelay(int frameIndex)
    {
        var delay = FrameDelays[Math.Min(frameIndex, FrameDelays.Length - 1)];
        if (delay < 0)
        {
            var frame = Image.Frames[frameIndex];
            return (int)(frame.Metadata.GetPngMetadata().FrameDelay.ToDouble() * 1000);
        }
        return delay;
    }
}
