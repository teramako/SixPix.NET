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
int w = -1, h = -1, f = -1;
var getData = false;
string infile = "", outfile = "";
const string MAP8_SIXEL = "Pq\"1;0;93;14#0;2;60;0;0#1;2;0;66;0#2;2;56;60;0#3;2;47;38;97#4;2;72;0;69#5;2;0;66;72#6;2;72;72;72#7;2;0;0;0#0!11~#1!12~#2!12~#3!12~#4!12~#5!12~#6!12~#7!10~-#0!11~#1!12~#2!12~#3!12~#4!12~#5!12~#6!12~#7!10~-#0!11B#1!12B#2!12B#3!12B#4!12B#5!12B#6!12B#7!10B\\";

foreach (var arg in args)
{
    string param = "";
    if (arg.StartsWith('-') || arg.StartsWith('/'))
    {
        param = arg.TrimStart('-', '/');
        switch (param[0])
        {
            case 'd':
            case 'D':
                getData = true;
                break;
            case 't':
                transp = Sixel.Transparency.None;
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

        if (getData)
        {
            var format = Sixel.GetFormat((Image<Rgba32>)image);
            Console.WriteLine("Image Format: " + format);
            fs.Seek(0, 0);
            var frames = Sixel.GetNumFrames((Image<Rgba32>)image);
            Console.WriteLine("  Num Frames: " + frames);
#if IMAGESHARP4
            fs.Seek(0, 0);
            var best = Sixel.GetBestFrame((Image<Rgba32>)image, null);
            Console.WriteLine("  Best Frame: " + best);
#endif
            fs.Seek(0, 0);
            var repeats = Sixel.GetRepeatCount((Image<Rgba32>)image);
            Console.WriteLine(" Num Repeats: " + repeats);
            Environment.Exit(frames);
        }

        // Encode: Image stream -> Sixel string (ReadOnlySpan<char>)
        var sixelString = Sixel.Encode(fs, new Size(w, h), transp, f);
#if SIXPIX_DEBUG
        var elapsed = DateTime.Now - start;
        Console.WriteLine($"Elapsed {elapsed.TotalMilliseconds} ms");
#endif

        if (!string.IsNullOrEmpty(outfile))
        {
            // Save image to a Sixel text file
            if (!outfile.Contains('.'))
                outfile += ".six";
            using var wf = new FileStream(outfile, FileMode.Create);
            Console.WriteLine($"Writing to file {wf.Name} ...");
            wf.Write(new UTF8Encoding(true).GetBytes(sixelString.ToArray()));
        }
        else
        {
            // Output to stdout
            Console.Out.WriteLine(sixelString);
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
    if (fileInfo.Extension is not ".six" and not ".sixel" and not ".txt")
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
    //----------------|---------10--------20--------30--------40--------50--------60--------70--------80
    //----------------|123456789|123456789|123456789|123456789|123456789|123456789|123456789|123456789|
    Console.WriteLine(MAP8_SIXEL);
    Console.WriteLine("[If you see colored bands above, your terminal supports Sixel!]");
    Console.WriteLine();
#if IMAGESHARP4 // ImageSharp v4.0 adds support for CUR and ICO files
    Console.WriteLine("Encoding usage:");
    Console.WriteLine("     SixPix.exe [/t|/T|/b] [/w:<width>] [/h:height>] <filein> [<fileout>]");
    Console.WriteLine(" /t              : Disable transparency (optional)");
    Console.WriteLine(" /T              : Make color at top-left (0,0) transparent (optional)");
    Console.WriteLine(" /b              : Make GIF or WebP background color transparent (optional)");
    Console.WriteLine(" /w:<width>      : Width in pixels (optional)");
    Console.WriteLine(" /h:<height>     : Height in pixels (optional)");
    Console.WriteLine(" /f:<frame>      : Specify frame for multi-frame images (optional)");
    Console.WriteLine(" <filein>        : Image file to encode to Sixel (required), supports BMP, CUR,");
    Console.WriteLine("                   GIF, ICO, JPEG, PBM, PNG, QOI, TGA, TIFF, and WebP");
#else
    Console.WriteLine(" <filein>        : Image file to encode to Sixel (required), supports BMP, GIF,");
    Console.WriteLine("                   JPEG, PBM, PNG, QOI, TGA, TIFF, and WebP");
#endif
    Console.WriteLine(" <fileout>[.six] : Output Sixel text filename (optional)");
    Console.WriteLine();
    Console.WriteLine("Decoding usage:");
    Console.WriteLine("     SixPix.exe <filein> <fileout>");
    Console.WriteLine(" <filein>        : Sixel text file to decode (required)");
    Console.WriteLine(" <fileout>[.png] : Output PNG image filename (required)");
    Console.WriteLine();
    Console.WriteLine("Informational usage:");
    Console.WriteLine("     SixPix.exe /f <filein>");
    Console.WriteLine(" /d              : Get image data (return code is number of frames)");
    Console.WriteLine(" <filein>        : Image file to read (required)");
    Console.WriteLine();
}
