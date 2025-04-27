#if IMAGESHARP4 // ImageSharp v4.0 adds support for CUR and ICO files
using System.Numerics;
#endif
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace SixPix;

public static partial class Sixel
{
    /// <summary>
    /// Decode Sixel string to <see cref="SixLabors.ImageSharp.Image"/>
    /// </summary>
    /// <param name="sixelString">Sixel string data</param>
    /// <returns>Decoded Image</returns>
    /// <exception cref="InvalidDataException">thrown when parsing Sixel data failed.</exception>
    public static Image<Rgba32> Decode(string sixelString)
    {
        using var mem = new MemoryStream(sixelString.Length);
        mem.Write(System.Text.Encoding.ASCII.GetBytes(sixelString));
        mem.Seek(0, SeekOrigin.Begin);
        return Decode(mem);
    }
    /// <param name="stream">Readable Stream containing Sixel data</param>
    /// <inheritdoc cref="Decode"/>
    public static Image<Rgba32> Decode(Stream stream)
    {
        List<Rgba32> _colorMap = [];
        int currentX = 0;
        int currentY = 0;
        int Width = 0;
        int Height = 0;

        int colorN = -1;
        int sixelBit;
        int repeatCount = 1;

        byte[] buffer = new byte[2];
        stream.Read(buffer);
        if (buffer[0] != 0x1B /* ESC */ || buffer[1] != 0x50 /* 'P' */)
        {
            throw new InvalidDataException($"Sixel must start with [ESC, 'P']");
        }

        int currentChar = stream.ReadByte();
        switch (currentChar)
        {
            case 0x71: // 'q'
                break;
            case 0x3B: // ';'
                break;
            default:
                do
                {
                    currentChar = stream.ReadByte();
                    // do nothing (ignore DCS)
                }
                while (stream.CanRead && currentChar != 0x71 /* 'q' */);
                break;
        }

        var canvasSize = new Size(200, 200);
        var resizeOption = new ResizeOptions()
        {
            Mode = ResizeMode.BoxPad,
            Position = AnchorPositionMode.TopLeft,
        };

#if IMAGESHARP4 // ImageSharp v4.0
        var image = new Image<Rgba32>(new Configuration(), canvasSize.Width, canvasSize.Height, Rgba32.FromScaledVector4(Color.White.ToScaledVector4()));
#else
        var image = new Image<Rgba32>(canvasSize.Width, canvasSize.Height, Color.White);
#endif


        DebugPrint("Start Sixel Data", lf: true);
        currentChar = stream.ReadByte();
        do
        {
            switch (currentChar)
            {
                case < 0:
                    throw new InvalidDataException($"Position = {stream.Position}");
                case 0x0a:
                case 0x0d:
                    break;
                case 0x1b: // ESC
                    var next = stream.ReadByte();
                    if (next == 0x5c) // '\' Sixel End sequence
                    {
                        if (image.Width != Width || image.Height != Height)
                        {
                            DebugPrint($"Crop {image.Width}x{image.Height} => {Width}x{Height}", ConsoleColor.Red, true);
                            image.Mutate(x => x.Crop(Width, Height));
                        }
                        return image;
                    }
                    throw new InvalidDataException($"Sixel must end with [ESC, '\']");
                case 0x21: // '!' Graphics Repeat Introducer
                    repeatCount = -1;
                    currentChar = ReadNumber(stream, ref repeatCount);
                    continue;
                case 0x22: // '"' Raster Attributes. see: https://vt100.net/docs/vt3xx-gp/chapter14.html
                    var param = new List<int>();
                    do
                    {
                        int paramNum = -1;
                        currentChar = ReadNumber(stream, ref paramNum);
                        param.Add(paramNum);
                    }
                    while (currentChar == 0x3B); // ';'
                    if (param.Count < 4)
                        throw new InvalidDataException($"Invalid Header: {string.Join(';', param)}");

                    canvasSize.Width = param[2];
                    canvasSize.Height = param[3];
                    DebugPrint($"Resize Image {image.Size} => {canvasSize}", lf: true);
                    resizeOption.Size = canvasSize;
                    image.Mutate(x => x.Resize(resizeOption));
                    continue;
                case 0x23: // '#'
                    colorN = -1;
                    currentChar = ReadNumber(stream, ref colorN);
                    if (currentChar == 0x3B) // ';' Enter ColorMap sequence
                    {
                        var (cSys, c1, c2, c3) = (-1, -1, -1, -1);
                        ReadNumber(stream, ref cSys);
                        ReadNumber(stream, ref c1);
                        ReadNumber(stream, ref c2);
                        currentChar = ReadNumber(stream, ref c3);
                        switch (cSys)
                        {
                            case 1: // HLS
                                _colorMap.Add(SixelColor.FromHLS(c1, c2, c3).ToRgba32());
                                break;
                            case 2: // RGB
                                _colorMap.Add(SixelColor.FromRgb(c1, c2, c3).ToRgba32());
                                break;
                            default:
                                throw new InvalidDataException($"Color map type should be 1 or 2: {cSys}");
                        }
                    }
                    continue;
                case 0x24: // '$'
                    currentX = 0;
                    break;
                case 0x2d: // '-'
                    currentX = 0;
                    currentY += 6;
                    if (canvasSize.Height < currentY + 6)
                    {
                        canvasSize.Height *= 2;
                        DebugPrint($"Resize Image Height {image.Size} => {canvasSize}", lf: true);
                        resizeOption.Size = canvasSize;
                        image.Mutate(x => x.Resize(resizeOption));
                    }
                    break;
                case > 0x3E and < 0x7F:
                    sixelBit = currentChar - 0x3F;

                    if (canvasSize.Width < currentX + repeatCount)
                    {
                        canvasSize.Width *= 2;
                        DebugPrint($"Resize Image Width {image.Size} => {canvasSize}", lf: true);
                        resizeOption.Size = canvasSize;
                        image.Mutate(x => x.Resize(resizeOption));
                    }
                    for (var x = currentX; x < currentX + repeatCount; x++)
                    {
                        var y = currentY;
                        for (var p = 0; p < 6; p++)
                        {
                            if ((sixelBit & (1 << p)) > 0)
                            {
                                image[x, y] = _colorMap[colorN];
                                if (Height < y + 1)
                                    Height = y + 1;
                            }
                            y++;
                        }
                    }
                    currentX += repeatCount;
                    if (Width < currentX)
                        Width = currentX;
                    repeatCount = 1;
                    break;
                default:
                    throw new InvalidDataException($"Invalid data at {stream.Position}: 0x{currentChar:x}");
            }

            currentChar = stream.ReadByte();
        }
        while (stream.CanRead);

        throw new InvalidDataException("Sixel data ended in the middle.");

    }
    private static int ReadNumber(Stream sr, ref int number)
    {
        int byteChar = -1;
        while (sr.CanRead)
        {
            byteChar = sr.ReadByte();
            if (byteChar is >= 0x30 and < 0x3A)
            {
                number = number >= 0 ? (number * 10) + byteChar - 0x30 : byteChar - 0x30;
                continue;
            }
            break;
        }
        return byteChar;
    }
}
