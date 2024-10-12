using SixPix;

Console.WriteLine("Hello, SixPix!");

if (args.Length == 0 || !Path.Exists(args[0]))
{
    Environment.Exit(1);
}

using var fs = File.OpenRead(args[0]);

var sixelString = Sixel.Encode(fs);
Console.Out.WriteLine(sixelString);

Environment.Exit(0);
