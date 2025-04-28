namespace SixPix.Encoder;

public interface IFrameMetadata
{
    /// <summary>
    /// Delay in milliseconds between the next drawing of each frame of the animation image.
    /// </summary>
    int FrameDelay { get; }
}
