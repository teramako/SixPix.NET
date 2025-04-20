using SixLabors.ImageSharp;

namespace SixPix;

public partial class Sixel
{
    private static Size? CellSize = null;
    private static Size? WindowPixelSize = null;
    private static Size? WindowCharSize = null;

    private const string CSI_DEVICE_ATTRIBUTES = "[c";
    private const string CSI_CELL_SIZE = "[16t";
    private const string CSI_WINDOW_PIXSIZE = "[14t";
    private const string CSI_WINDOW_CHARSIZE = "[18t";

    /// <summary>
    /// Check whether current terminal support Sixel
    /// <returns>bool</returns>
    /// </summary>
    public static bool IsSupported()
    {

        // Should get something like: ^[[?61;4;6;7;14;21;22;23;24;28;32;42c
        // The "4" indicates Sixel support
        var response = GetCtrlSeqResponse(CSI_DEVICE_ATTRIBUTES);

        if (response.Contains(";4;"))
            return true;

        return false;
    }

    /// <summary>
    /// Get the cell size of the terminal in pixel-sixel size.
    /// </summary>
    /// <returns><see cref="SixLabors.ImageSharp.Size/> in pixels that will fit in a single character cell.</returns>
    /// <remarks>The response to the command will look like [6;20;10t where the 20 is height and 10 is width.  Is the 6 the terminal class?</remarks>
    public static Size? GetCellSize()
    {
        // cache result, doesn't change with terminal size
        if (CellSize != null)
            return CellSize;

        var response = GetCtrlSeqResponse(CSI_CELL_SIZE);
        try
        {
            var parts = response.Split(';', 't');
            return new()
            {
                Width = int.Parse(parts[2]),
                Height = int.Parse(parts[1])
            };
        }
        catch
        {
            // Return the default Windows Terminal size if we can't get the size from the terminal.
            return new(10, 20);
        }
    }


    /// <summary>
    /// Get the window size in pixels.
    /// </summary>
    /// <returns><see cref="SixLabors.ImageSharp.Size/> in pixels</returns>
    public static Size? GetWindowPixelSize()
    {
        // don't cache result, since the terminal can be resized
        var response = GetCtrlSeqResponse(CSI_WINDOW_PIXSIZE);
        try
        {
            var parts = response.Split(';', 't');
            return new()
            {
                Width = int.Parse(parts[2]),
                Height = int.Parse(parts[1]),
            };
        }
        catch
        {
            return new(0, 0);
        }
    }

    /// <summary>
    /// Get the window size in characters.
    /// </summary>
    /// <returns><see cref="SixLabors.ImageSharp.Size/> in characters</returns>
    /// <remarks>Equivalent to (Console.WindowWidth, Console.WindowHeight)</remarks>
    public static Size? GetWindowCharSize()
    {
        // don't cache result, since the terminal can be resized
        return new(Console.WindowWidth, Console.WindowHeight);

        /*
        var response = GetCtrlSeqResponse(CSI_WINDOW_CHARSIZE);
        try
        {
            var parts = response.Split(';', 't');
            return new()
            {
                Width = int.Parse(parts[2]),
                Height = int.Parse(parts[1]),
            };
        }
        catch
        {
            return new(0, 0);
        }
        */
    }

    /// <summary>
    /// Get the response to an ANSI control sequence.
    /// <returns>string response</returns>
    /// </summary>
    public static string GetCtrlSeqResponse(string ctrlSeq)
    {
        char? c;
        var response = "";

        // Console.Write($"{ESC}{ctrlSeq}{ST}");
        Console.Write($"{ESC}{ctrlSeq}");
        do
        {
            c = Console.ReadKey(true).KeyChar;
            response += c;
        }
        while (c != 'c' && Console.KeyAvailable);

        return response;
    }
}
