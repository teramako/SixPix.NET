using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixPix;
using System.Text;

#if SIXPIX_DEBUG
Console.WriteLine("Hello, SixPix!");
#endif

if (args.Length == 0)
{
    PrintUsage();
    Environment.Exit(1);
}

Sixel.Transparency transp = Sixel.Transparency.Default;
int w = -1, h = -1, f = 0, rate = 10;
bool getData = false, anim = false, animForever = false;
string infile = "", outfile = "";
const string MAP8_SIXEL = "Pq\"1;1;93;14#0;2;60;0;0#1;2;0;66;0#2;2;56;60;0#3;2;47;38;97#4;2;72;0;69#5;2;0;66;72#6;2;72;72;72#7;2;0;0;0#0!11~#1!12~#2!12~#3!12~#4!12~#5!12~#6!12~#7!10~-#0!11~#1!12~#2!12~#3!12~#4!12~#5!12~#6!12~#7!10~-#0!11B#1!12B#2!12B#3!12B#4!12B#5!12B#6!12B#7!10B\\";

foreach (var arg in args)
{
    string param = "";
    if (arg.StartsWith('-') || arg.StartsWith('/'))
    {
        param = arg.TrimStart('-', '/');
        switch (param[0])
        {
            case 'a':
                anim = true;
                transp = Sixel.Transparency.None;
                break;
            case 'A':
                anim = true;
                transp = Sixel.Transparency.None;
                animForever = true;
                break;
            case 'd':
            case 'D':
                getData = true;
                break;
            case 't':
                transp = transp == Sixel.Transparency.None ? Sixel.Transparency.Default : Sixel.Transparency.None;
                break;
            case 'T':
                transp = Sixel.Transparency.TopLeft;
                break;
            case 'b':
            case 'B':
                transp = Sixel.Transparency.Background;
                break;
            case 'w':
            case 'W':
                if (param.Contains('='))
                    _ = int.TryParse(param[(param.IndexOf('=') + 1)..], out w);
                else if (param.Contains(':'))
                    _ = int.TryParse(param[(param.IndexOf(':') + 1)..], out w);
                break;
            case 'h':
            case 'H':
                if (param.Contains('='))
                    _ = int.TryParse(param[(param.IndexOf('=') + 1)..], out h);
                else if (param.Contains(':'))
                    _ = int.TryParse(param[(param.IndexOf(':') + 1)..], out h);
                break;
            case 'f':
            case 'F':
                if (param.Contains('='))
                    _ = int.TryParse(param[(param.IndexOf('=') + 1)..], out f);
                else if (param.Contains(':'))
                    _ = int.TryParse(param[(param.IndexOf(':') + 1)..], out f);
                if (f < 0)
                    f = 0;
                break;
            case 'r':
            case 'R':
                if (param.Contains('='))
                    _ = int.TryParse(param[(param.IndexOf('=') + 1)..], out rate);
                else if (param.Contains(':'))
                    _ = int.TryParse(param[(param.IndexOf(':') + 1)..], out rate);
                if (rate < 0)
                    rate = 0;
                break;
            case 'o':    // output filename (explicit instead of based on position)
            case 'O':
                if (param.Contains('='))
                    outfile = param[(param.IndexOf('=') + 1)..];
                else if (param.Contains(':'))
                    outfile = param[(param.IndexOf(':') + 1)..];
                break;
            case 'i':    // input filename (explicit instead of based on position)
            case 'I':
                if (param.Contains('='))
                    infile = param[(param.IndexOf('=') + 1)..];
                else if (param.Contains(':'))
                    infile = param[(param.IndexOf(':') + 1)..];
                break;
            default:
                Console.Error.WriteLine("Error: Unrecognized parameter '" + param + "'");
                Environment.Exit(1);
                break;
        }
    }
    else if (string.IsNullOrEmpty(infile))
        infile = arg;
    else
        outfile = arg;
}
if (!Path.Exists(infile))
{
    if (string.IsNullOrEmpty(infile))
        Console.Error.WriteLine("Error: No input filename provided.");
    else
        Console.Error.WriteLine("Error: File not found: " + infile);
    PrintUsage();
    Environment.Exit(1);
}

var fileInfo = new FileInfo(infile);
if (IsBinary(infile))
{
    var start = DateTime.Now;

    try
    {
        using var fs = fileInfo.OpenRead();
        using var image = Image.Load(fs);

        fs.Seek(0, 0);
        var format = Sixel.GetFormat((Image<Rgba32>)image);
        fs.Seek(0, 0);
        var numFrames = Sixel.GetNumFrames((Image<Rgba32>)image);
        if (f >= numFrames)
        {
            Console.Error.WriteLine("Error: Specified frame does not exist (index starts at 0).");
            getData = true;
        }
#if IMAGESHARP4
        fs.Seek(0, 0);
        var best = Sixel.GetBestFrame((Image<Rgba32>)image, null);
#endif
        fs.Seek(0, 0);
        var numRepeats = Sixel.GetRepeatCount((Image<Rgba32>)image);

        if (getData)
        {
            Console.WriteLine("Image Format: " + format);
            Console.WriteLine("  Num Frames: " + numFrames);
#if IMAGESHARP4
            Console.WriteLine("  Best Frame: " + best);
#endif
            Console.WriteLine(" Num Repeats: " + numRepeats);
            Environment.Exit(numFrames);
        }

        List<string> frames = [];
        for (int frame = 0; frame < numFrames; frame++)
        {
            fs.Seek(0, 0);
            // Encode: Image stream -> Sixel string (ReadOnlySpan<char>)
            frames.Add(Sixel.Encode(fs, new Size(w, h), transp, frame).ToString());
#if SIXPIX_DEBUG
            if (!anim)
            {
                var elapsed = DateTime.Now - start;
                Console.WriteLine($"Elapsed {elapsed.TotalMilliseconds} ms");
            }
#endif
            if (!string.IsNullOrEmpty(outfile))
            {
                // Save image to Sixel text file
                if (anim)
                    outfile += frame;
                if (!outfile.Contains('.'))
                    outfile += ".six";
                using var wf = new FileStream(outfile, FileMode.Create);
                Console.WriteLine($"Writing to file {wf.Name} ...");
                wf.Write(new UTF8Encoding(true).GetBytes(frames[frame]));
            }
        }

        if (!string.IsNullOrEmpty(outfile))
            Environment.Exit(0);

        int x = 0, y = 0;
        if (numRepeats < 1)
            numRepeats = 1;
        if (anim)
        {
            Console.Clear();
            (x, y) = Console.GetCursorPosition();
            if (animForever)
            {
                Console.WriteLine("Press Ctrl+C to stop.");
                y++;
            }
        }

        for (int repeat = 0; repeat < numRepeats; repeat++)
        {
            for (; f < numFrames; f++)
            {
                if (anim)
                    Console.SetCursorPosition(x, y);

                // Output to stdout
                Console.WriteLine(frames[f]);

                if (anim)
                    Thread.Sleep(rate * 10);
                else
                    break;
            }
            if (animForever)
                repeat = 0;
        }
    }
    catch (Exception e)
    {
        Console.Error.WriteLine($"Error: {e.Message}");
#if SIXPIX_DEBUG
        Console.Error.WriteLine(e.StackTrace);
#endif
    }
}
else
{
    if (fileInfo.Extension is not ".six" and not ".sixel" and not ".txt" and not ".out")
        Console.WriteLine("Unknown filetype, attempting to decode from Sixel string ...");

    if (string.IsNullOrEmpty(outfile))
    {
        Console.Error.WriteLine("Error: No output filename provided. Required for Sixel decoding.");
        PrintUsage();
        Environment.Exit(1);
    }

    if (!outfile.Contains('.'))
        outfile += ".six";

    var start = DateTime.Now;
    // Decode: Sixel stream -> Image<Rgba32>
    try
    {
        using var fs = fileInfo.OpenRead();
        using var image = Sixel.Decode(fs);
#if SIXPIX_DEBUG
        var elapsed = DateTime.Now - start;
        Console.WriteLine($"Elapsed {elapsed.TotalMilliseconds} ms");
#endif

        if (!string.IsNullOrEmpty(outfile))
        {
            // Save image to a file as png
            if (!outfile.Contains('.'))
                outfile += ".png";
            using var wf = new FileStream(outfile, FileMode.Create);
            Console.WriteLine($"Writing to file {wf.Name} ...");
            image.Save(wf, new PngEncoder());
        }
    }
    catch (Exception e)
    {
        Console.Error.WriteLine($"Error: {e.Message}");
#if SIXPIX_DEBUG
        Console.Error.WriteLine(e.StackTrace);
#endif
    }
}
Environment.Exit(0);

static bool IsBinary(string filePath)
{
    int numNul = 0;

    using var sr = new StreamReader(filePath);
    for (var i = 0; i < 8000; i++)
    {
        if (sr.EndOfStream)
            return false;

        if ((char)sr.Read() == '\0')
        {
            numNul++;

            if (numNul >= 1)
                return true;
        }
        else
            numNul = 0;
    }

    return false;
}

static void PrintUsage()
{
    Console.WriteLine(MAP8_SIXEL);
    if (Sixel.IsSupported())
    {
        var cellSize = Sixel.GetCellSize();
        var windowCharSize = Sixel.GetWindowCharSize();
        var windowPixelSize = Sixel.GetWindowPixelSize();

        Console.WriteLine($"Sixel is supported! [Cell Size:{cellSize?.Width}x{cellSize?.Height}; " +
            $"Current Window:{windowPixelSize?.Width}x{windowPixelSize?.Height}px, {windowCharSize?.Width}x{windowCharSize?.Height}ch]");
    }
    else
        Console.WriteLine("If you see colored bands above, your terminal supports Sixel.");

    Console.WriteLine();
    //----------------|---------10--------20--------30--------40--------50--------60--------70--------80
    //----------------|123456789|123456789|123456789|123456789|123456789|123456789|123456789|123456789|
    Console.WriteLine("Encoding usage:");
    Console.WriteLine("     SixPix.exe [/t|/T|/b] [/w:<W>] [/h:<H>] [/a|/A|/f:<F>] [/r:R] <in> [<out>]");
    Console.WriteLine(" /t          : Disable transparency, or enable for animations (optional)");
    Console.WriteLine(" /T          : Make color at top-left (x=0,y=0) transparent (optional)");
    Console.WriteLine(" /b          : Make GIF or WebP background color transparent (optional)");
    Console.WriteLine(" /w:<Width>  : Width in pixels (optional)");
    Console.WriteLine(" /h:<Height> : Height in pixels (optional)");
    Console.WriteLine(" /a          : Animate the frames of a multi-frame image (optional)");
    Console.WriteLine(" /A          : Animate forever, Ctrl+C to stop (optional)");
    Console.WriteLine(" /f:<Frame>  : Display a single frame of a multi-frame image (optional)");
    Console.WriteLine(" /r:<Rate>   : Animation framerate (in frames per second), default=10");
#if IMAGESHARP4 // ImageSharp v4.0 adds support for CUR and ICO files
    Console.WriteLine(" <in>        : Image filename to encode to Sixel (required), supports BMP, CUR,");
    Console.WriteLine("               GIF, ICO, JPEG, PBM, PNG, QOI, TGA, TIFF, and WebP");
#else
    Console.WriteLine(" <in>        : Image filename to encode to Sixel (required), supports BMP, GIF,");
    Console.WriteLine("               JPEG, PBM, PNG, QOI, TGA, TIFF, and WebP");
#endif
    Console.WriteLine(" <out>[.six] : Output Sixel text filename (optional)");
    Console.WriteLine();
    Console.WriteLine("Decoding usage:");
    Console.WriteLine("     SixPix.exe <in> <out>");
    Console.WriteLine(" <in>        : Sixel text file to decode (required)");
    Console.WriteLine(" <out>[.png] : Output PNG image filename (required)");
    Console.WriteLine();
    Console.WriteLine("Informational usage:");
    Console.WriteLine("     SixPix.exe /d <in>");
    Console.WriteLine(" /d          : Get image data (required), return code is number of frames");
    Console.WriteLine(" <in>        : Image filename to read (required)");
    Console.WriteLine();
}
