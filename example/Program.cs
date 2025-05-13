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

Transparency transp = Transparency.Default;
int w = -1, h = -1, f = -1, delay = -1;
Rgba32 bgColor = new();
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
                break;
            case 'A':
                anim = true;
                animForever = true;
                break;
            case 'g':
            case 'G':
                getData = true;
                break;
            case 't':
                transp = Transparency.None;
                break;
            case 'T':
                transp = Transparency.TopLeft;
                break;
            case 'b':
            case 'B':
                transp = Transparency.Background;
                break;
            case 'c':
            case 'C':
                transp = Transparency.None;
                var o = ' ';
                if (param.Contains('='))
                    o = '=';
                else if (param.Contains(':'))
                    o = ':';
                else
                    break;
                string s = param[(param.IndexOf(o) + 1)..];
                if (s.StartsWith('#'))
                    s = s[1..];
                else if (s.StartsWith("0x"))
                    s = s[2..];
                if (s.Length > 2 && s.Length < 9)
                {
                    if (s.Length == 5 || s.Length == 7)
                        s = '0' + s;
                    if (Rgba32.TryParseHex(s, out bgColor))
                        break;
                }
                Console.WriteLine("Error: Bad color value specified (must be in hexadecimal).");
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
            case 'd':
            case 'D':
                if (param.Contains('='))
                    _ = int.TryParse(param[(param.IndexOf('=') + 1)..], out delay);
                else if (param.Contains(':'))
                    _ = int.TryParse(param[(param.IndexOf(':') + 1)..], out delay);
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

    // Don't allow file output forever
    if (!string.IsNullOrEmpty(outfile))
        animForever = false;
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
        using var image = Image.Load<Rgba32>(fs);
        using var sixelEncoder = Sixel.CreateEncoder(image)
                                      .Resize(width: w, height: h);

        // Reverse /t logic when displaying animations
        if (anim && sixelEncoder.ReverseTransparencyOnAnimate)
        {
            if (transp == Transparency.None)
                transp = Transparency.Default;
            else if (transp == Transparency.Default)
                transp = Transparency.None;
        }
        sixelEncoder.TransparencyMode = transp;

        if (bgColor != default)
            Sixel.BackgroundColor = bgColor;

        if (f >= sixelEncoder.FrameCount)
        {
            Console.Error.WriteLine("Error: Specified frame does not exist (index starts at 0).");
            getData = true;
        }
#if IMAGESHARP4
        var best = Sixel.GetBestFrame(sixelEncoder.Image, null);
#endif
        if (getData)
        {
            (var cw, var ch) = sixelEncoder.CanvasSize;
            Console.WriteLine("Image Format: " + sixelEncoder.Format);
            Console.WriteLine("  Image Size: " + cw + "x" + ch);
            Console.WriteLine("  Num Frames: " + sixelEncoder.FrameCount);
#if IMAGESHARP4
            Console.WriteLine("  Best Frame: " + best);
#endif
            Console.WriteLine(" Num Repeats: " + sixelEncoder.RepeatCount);
                Console.Write("Frame Delays: [ ");
            for (var fr = 0; fr < sixelEncoder.FrameCount; fr++)
            {
                Console.Write(sixelEncoder.GetFrameDelay(fr) + " ");
            }
            Console.WriteLine("]");
            Environment.Exit(sixelEncoder.FrameCount);
        }

        if (!string.IsNullOrEmpty(outfile))
        {
            if (!anim) // Write to one file
            {
                var sixelString = sixelEncoder.EncodeFrame(f);
#if SIXPIX_DEBUG
                var elapsed = DateTime.Now - start;
                Console.WriteLine($"Elapsed {elapsed.TotalMilliseconds} ms");
#endif
                // Save image to Sixel text file
                var thisOutfile = outfile;
                if (!thisOutfile.Contains('.'))
                    thisOutfile += ".six";
                using var wf = new FileStream(thisOutfile, FileMode.Create);
                Console.WriteLine($"Writing to file {wf.Name} ...");
                wf.Write(new UTF8Encoding(true).GetBytes(sixelString));
            }
            else // write each frame to a file
            {
                var frameIndex = 0;
                foreach (var sixelString in sixelEncoder.EncodeFrames())
                {
#if SIXPIX_DEBUG
                    var elapsed = DateTime.Now - start;
                    Console.WriteLine($"Elapsed {elapsed.TotalMilliseconds} ms");
#endif
                    // Save image to Sixel text file
                    var thisOutfile = outfile;
                    // Add frame number to filename if exporting multiple frames
                    if (anim)
                        thisOutfile += frameIndex;
                    if (!thisOutfile.Contains('.'))
                        thisOutfile += ".six";
                    using var wf = new FileStream(thisOutfile, FileMode.Create);
                    Console.WriteLine($"Writing to file {wf.Name} ...");
                    wf.Write(new UTF8Encoding(true).GetBytes(sixelString));

                    frameIndex++;
                }
            }
            Environment.Exit(0);
        }

        if (!anim)
        {
#if IMAGESHARP4
            if (f < 0 && best >= 0)
                f = best;
#endif
            if (f < 0)
                f = 0;
            Console.WriteLine(sixelEncoder.EncodeFrame(f));
            Environment.Exit(0);
        }

        if (sixelEncoder.FrameCount < 2)
        {
            Console.Error.WriteLine("Error: Cannot animate a single-frame file.");
            Environment.Exit(1);
        }

        // Start animation
        Console.WriteLine("Press 'Ctrl+C', 'c' or 'q' to stop.");
        using var ct = new CancellationTokenSource();
        sixelEncoder.SetFrameDelays(delay >= 0 ? delay : -1);
        var t1 = sixelEncoder.Animate(animForever ? 0 : 1,
                                      f < 0 ? 0 : f,
                                      -1,
                                      ct.Token);
        var t2 = Task.Run(() =>
        {
            ConsoleKeyInfo keyInfo;
            do
            {
                keyInfo = Console.ReadKey(true);
                if (keyInfo.KeyChar is 'c' or 'q')
                {
                    ct.CancelAsync();
                    break;
                }
            }
            while (true);
        });
        t1.Wait();
    }
    catch (AggregateException e)
    {
        if (e.InnerException is TaskCanceledException ex)
            Console.Error.WriteLine("Canceled.");
        else
            throw;
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

        Console.WriteLine($"Sixel is supported!");
        Console.WriteLine($"Background:#{Sixel.BackgroundColor}; " +
            $"Sync Output:{Sixel.IsSyncSupported()}; " +
            $"Cell Size:{cellSize.Width}x{cellSize.Height}; " +
            $"Current Window:{windowPixelSize.Width}x{windowPixelSize.Height}px, " +
            $"{windowCharSize.Width}x{windowCharSize.Height}ch");
    }
    else
        Console.WriteLine("If you see colored bands above, your terminal supports Sixel.");

    Console.WriteLine();
    //----------------|---------10--------20--------30--------40--------50--------60--------70--------80
    //----------------|123456789|123456789|123456789|123456789|123456789|123456789|123456789|123456789|
    Console.WriteLine("Encoding usage:");
    Console.WriteLine("    SixPix.exe [params] <in> [<out>]");
    Console.WriteLine(" /t          : Disable transparency, or enable when animating (optional)");
    Console.WriteLine(" /T          : Make color at top-left (x=0,y=0) transparent (optional)");
    Console.WriteLine(" /b          : Make GIF or WebP background color transparent (optional)");
    Console.WriteLine(" /c:<Color>  : Specify a background color in hexadecimal (RRGGBB) (optional)");
    Console.WriteLine(" /w:<Width>  : Width in pixels (optional)");
    Console.WriteLine(" /h:<Height> : Height in pixels (optional)");
    Console.WriteLine(" /a          : Animate the frames of a multi-frame image (optional),");
    Console.WriteLine("               With <out> specified, encode all frames and append frame number");
    Console.WriteLine(" /A          : Animate forever (optional), 'Ctrl+C', 'c' or 'q' to stop");
    Console.WriteLine(" /f:<Frame>  : Display a single frame of a multi-frame image (optional)");
    Console.WriteLine(" /d:<Delay>  : Animation delay (in milliseconds)");
#if IMAGESHARP4 // ImageSharp v4.0 adds support for CUR and ICO files
    Console.WriteLine(" <in>        : Image filename to encode to Sixel (required), supports BMP, CUR,");
    Console.WriteLine("               GIF, ICO, JPEG, PBM, PNG, QOI, TGA, TIFF, and WebP");
#else
    Console.WriteLine(" <in>        : Image filename to encode to Sixel (required), supports BMP, GIF,");
    Console.WriteLine("               JPEG, PBM, PNG, QOI, TGA, TIFF, and WebP");
#endif
    Console.WriteLine(" <out>[.six] : Output Sixel text filename (optional), otherwise display image(s)");
    Console.WriteLine();
    Console.WriteLine("Decoding usage:");
    Console.WriteLine("    SixPix.exe <in> <out>");
    Console.WriteLine(" <in>        : Sixel text file to decode (required)");
    Console.WriteLine(" <out>[.png] : Output PNG image filename (required)");
    Console.WriteLine();
    Console.WriteLine("Informational usage:");
    Console.WriteLine("    SixPix.exe /g <in>");
    Console.WriteLine(" /g          : Get image data (required), return code is number of frames");
    Console.WriteLine(" <in>        : Image filename to read (required)");
    Console.WriteLine();
}
