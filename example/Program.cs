using SixLabors.ImageSharp.Formats.Png;
using SixPix;

Console.WriteLine("Hello, SixPix!");

if (args.Length == 0 || !Path.Exists(args[0]))
{
    Environment.Exit(1);
}

var fileInfo = new FileInfo(args[0]);
using var fs = fileInfo.OpenRead();
switch (fileInfo.Extension)
{
    case ".png":
    case ".jpg":
    case ".gif":
    case ".bmp":
        {
            var start = DateTime.Now;
            // Encode: Image stream -> Sixel string (ReadOnlySpan<char>)
            var sixelString = Sixel.Encode(fs);
            var elaps = DateTime.Now - start;
            Console.WriteLine($"Elaps {elaps.TotalMilliseconds} ms");

            // Output to stdout
            Console.Out.WriteLine(sixelString);

            Environment.Exit(0);
        }
        break;
    default:
        {
            var start = DateTime.Now;
            // Decode: Sixel stream -> Image<Rgb24>
            using var image = Sixel.Decode(fs);
            var elaps = DateTime.Now - start;
            Console.WriteLine($"Elaps {elaps.TotalMilliseconds} ms");

            // Save image to a file as png
            using var wf = new FileStream("./test.png", FileMode.Create);
            Console.WriteLine(wf.Name);
            image.Save(wf, new PngEncoder());
        }
        Environment.Exit(0);
        break;
}
