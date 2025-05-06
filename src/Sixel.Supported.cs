using System.Globalization;
using System.Text;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace SixPix;

public static partial class Sixel
{
    private static Size? CellSize;

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
        DebugPrint($"IsSupported: ^[{CSI_DEVICE_ATTRIBUTES} => ", ConsoleColor.DarkGray);
        var response = GetCtrlSeqResponse(CSI_DEVICE_ATTRIBUTES);

        return response.Contains(";4;", StringComparison.Ordinal)
            || response.EndsWith(";4", StringComparison.Ordinal);
    }

    /// <summary>
    /// Get the cell size of the terminal in pixel-sixel size.
    /// </summary>
    /// <returns><see cref="Size"/> in pixels that will fit in a single character cell.</returns>
    /// <remarks>The response to the command will look like [6;20;10t where the 20 is height and 10 is width.  Is the 6 the terminal class?</remarks>
    public static Size GetCellSize()
    {
        // cache result, doesn't change with terminal size
        if (CellSize is not null)
            return CellSize.Value;

        DebugPrint($"GetCellSize: ^[{CSI_CELL_SIZE} => ", ConsoleColor.DarkGray);
        var response = GetCtrlSeqResponse(CSI_CELL_SIZE);
        try
        {
            Span<Range> ranges = new Range[3];
            response.Split(ranges, ';');
            CellSize = new()
            {
                Width = int.Parse(response[ranges[2]], CultureInfo.InvariantCulture),
                Height = int.Parse(response[ranges[1]], CultureInfo.InvariantCulture)
            };
        }
        catch
        {
            // Return the default Windows Terminal size if we can't get the size from the terminal.
            return new(10, 20);
        }
        return CellSize.Value;
    }


    /// <summary>
    /// Get the window size in pixels.
    /// </summary>
    /// <returns><see cref="Size"/> in pixels</returns>
    public static Size GetWindowPixelSize()
    {
        // don't cache result, since the terminal can be resized
        DebugPrint($"GetWindowPixelSize: ^[{CSI_WINDOW_PIXSIZE} => ", ConsoleColor.DarkGray);
        var response = GetCtrlSeqResponse(CSI_WINDOW_PIXSIZE);
        try
        {
            Span<Range> ranges = new Range[3];
            response.Split(ranges, ';');
            return new()
            {
                Width = int.Parse(response[ranges[2]], CultureInfo.InvariantCulture),
                Height = int.Parse(response[ranges[1]], CultureInfo.InvariantCulture)
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
    /// <returns><see cref="Size"/> in characters</returns>
    /// <remarks>Equivalent to (Console.WindowWidth, Console.WindowHeight)</remarks>
    public static Size GetWindowCharSize()
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

    private static Color _termBG = Color.Transparent;

    /// <summary>
    /// Background color.
    /// The transparent color is replaced or blend by this color when <see cref="Transparency"/> is <c>None</c>
    /// </summary>
    public static Color BackgroundColor
    {
        get
        {
            if (_termBG != Color.Transparent)
                return _termBG;

            // ^[]11;rgb:2828/2c2c/3434^[\
            DebugPrint("GetTerminalBackgroundColor: ^[]11;?\\G  => ", ConsoleColor.DarkGray);
            var response = GetCtrlSeqResponse($"]11;?{(char)0x07}", '\\');
            var start = response.IndexOf(':') + 1;
            if (start < 1)
                return _termBG;

            var code = response[start..];
            Span<Range> rgbRange = new Range[3];
            if (code.Split(rgbRange, '/') != 3)
                return _termBG;

            _termBG = Color.FromPixel(new Rgb48(ushort.Parse(code[rgbRange[0]], NumberStyles.HexNumber, CultureInfo.InvariantCulture),
                                                ushort.Parse(code[rgbRange[1]], NumberStyles.HexNumber, CultureInfo.InvariantCulture),
                                                ushort.Parse(code[rgbRange[2]], NumberStyles.HexNumber, CultureInfo.InvariantCulture)));
            return _termBG;
        }
        set => _termBG = value;
    }

    /// <summary>
    /// Get the response to an ANSI control sequence.
    /// </summary>
    /// <returns>string response</returns>
    public static ReadOnlySpan<char> GetCtrlSeqResponse(string ctrlSeq, char? endChar = null)
    {
        char end = endChar ?? ctrlSeq[^1];
        var response = new StringBuilder();

        Task.WaitAll([
            Task.Run(() =>
            {
                do
                {
                    char c = Console.ReadKey(true).KeyChar;
                    DebugPrint($"{(char.IsControl(c) ? $"\\{(char)(Convert.ToByte(c) + 0x40)}" : c)}",
                               char.IsControl(c) ? ConsoleColor.Magenta : ConsoleColor.Red);
                    if (c == end)
                        break;
                    if (!char.IsControl(c))
                        response.Append(c);
                }
                while (Console.KeyAvailable);
                DebugPrint(string.Empty, lf: true);
            }),
            Task.Run(() =>
            {
                Console.Out.Write($"{ESC}{ctrlSeq}");
            })
        ]);
        return response.ToString();
    }
}
