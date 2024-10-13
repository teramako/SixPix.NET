using System.Diagnostics;

namespace SixPix;

public partial class Sixel
{
    [Conditional("DEBUG")]
    static void DebugPrint(ReadOnlySpan<char> msg, ConsoleColor fg = ConsoleColor.Magenta, bool lf = false)
    {
        var currentFg = Console.ForegroundColor;
        Console.ForegroundColor = fg;
        if (lf)
            Console.Error.WriteLine(msg);
        else
            Console.Error.Write(msg);
        Console.ForegroundColor = currentFg;
    }
}
