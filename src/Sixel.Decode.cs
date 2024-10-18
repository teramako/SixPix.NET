using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace SixPix;

public partial class Sixel
{

    /// <summary>
    /// </summary>
    /// <param name="stream">Readable stream contains Sixel data</param>
    /// <returns>Decoded result</returns>
    /// <exception cref="InvalidDataException">thrown when parsing Sixel data was failed.</exception>
    public static Image<Rgb24> Decode(Stream stream)
    {
        List<Rgb24> _colorMap = new List<Rgb24>();
        Image<Rgb24>? image = null;

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
            throw new InvalidDataException($"Sixel must be started with [ESC, 'P']");
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
                        if (image is not null)
                            return image;
                        throw new InvalidDataException("Image is null. May be sixcel data is empty.");
                    }
                    throw new InvalidDataException($"Sixel must be end with [ESC, '\']");
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
                        throw new InvalidDataException($"Invalie Header: {string.Join(';', param)}");

                    Width = param[2];
                    Height = param[3];
                    image = new Image<Rgb24>(Width, Height);
                    DebugPrint($"New Image {Width}x{Height}", lf: true);
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
                                _colorMap.Add(HLStoRGB(c1, c2, c3));
                                break;
                            case 2: // RGB
                                var rgb = new Rgb24(
                                        (byte)(c1 * 0xFF / 100),
                                        (byte)(c2 * 0xFF / 100),
                                        (byte)(c3 * 0xFF / 100));
                                _colorMap.Add(rgb);
                                break;
                            default:
                                throw new InvalidDataException($"color map type should be 1 or 2: {cSys}");
                        }
                    }
                    continue;
                case 0x24: // '$'
                    currentX = 0;
                    break;
                case 0x2d: // '-'
                    currentX = 0;
                    currentY += 6;
                    break;
                case > 0x3E and < 0x7F:
                    sixelBit = currentChar - 0x3F;
                    if (repeatCount < 0)
                        repeatCount = 1;

                    for (var x = currentX; x < currentX + repeatCount; x++)
                    {
                        for (var p = 0; p < 6; p++)
                        {
                            var y = currentY + p;
                            if ((sixelBit & (1 << p)) > 0)
                            {
                                image![x, y] = _colorMap[colorN];
                            }
                        }
                    }
                    currentX += repeatCount;
                    repeatCount = -1;
                    break;
                default:
                    throw new InvalidDataException($"Invalid data at {stream.Position}: {Convert.ToChar(currentChar)}");
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
            if (byteChar >= 0x30 && byteChar < 0x3A)
            {
                if (number >= 0)
                {
                    number = number * 10 + byteChar - 0x30;
                }
                else
                {
                    number = byteChar - 0x30;
                }
                continue;
            }
            break;
        }
        return byteChar;
    }

    private static Rgb24 HLStoRGB(int h, int l, int s)
    {
        double r; double g; double b;
        double max; double min;

        if (l > 50)
        {
            max = l + s * (1.0 - l / 100.0);
            min = l - s * (1.0 - l / 100.0); 
        }
        else
        {
            max = l + (s * l) / 100.0;
            min = l - (s * l) / 100.0;
        }

        h = (h + 240) % 360;

        switch (h)
        {
            case < 60:
                r = max;
                g = min + (max - min) * h / 60.0;
                b = min;
                break;
            case < 120:
                r = min + (max - min) * (120 - h) / 60.0;
                g = max;
                b = min;
                break;
            case < 180:
                r = min;
                g = max;
                b = min + (max - min) * (h - 120) / 60.0;
                break;
            case < 240:
                r = min;
                g = min + (max - min) * (240 - h) / 60.0;
                b = max;
                break;
            case < 300:
                r = min + (max - min) * (h - 240) / 60.0;
                g = min;
                b = max;
                break;
            default:
                r = max;
                g = min;
                b = min + (max - min) * (360 - h) / 60.0;
                break;
        }

        return new Rgb24((byte)(r * 0xFF / 100),
                         (byte)(g * 0xFF / 100),
                         (byte)(b * 0xFF / 100));
    }
}
