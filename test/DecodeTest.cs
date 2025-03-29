using SixLabors.ImageSharp.PixelFormats;
using SixPix;

namespace test;

[TestClass]
public class DecodeTest
{
    private const char ESC = '\x1b';

    [TestMethod("[Decode] RGB color")]
    public void TestMethod1()
    {
        var sixelString = $"""
            {ESC}P7;1;q"1;1;5;12
            #0;2;0;0;0
            #1;2;0;0;100
            #2;2;50;0;100
            #3;2;100;0;100
            #4;2;100;0;50
            #5;2;100;0;0
            #6;2;100;50;0
            #7;2;100;100;0
            #8;2;50;100;0
            #9;2;0;100;0
            #10;2;0;100;50
            #11;2;0;100;100
            #12;2;0;50;100
            #13;2;100;100;100
            #0!10~-
            #1!10~-
            #2!10~-
            #3!10~-
            #4!10~-
            #5!10~-
            #6!10~-
            #7!10~-
            #8!10~-
            #9!10~-
            #10!10~-
            #11!10~-
            #12!10~-
            #13!10~-
            #13!10@$
            #0!5?!5@
            {ESC}\
            """;
        Console.WriteLine(sixelString);
        using var image = Sixel.Decode(sixelString);

        Assert.AreEqual(10, image.Width);
        Assert.AreEqual((14 * 6) + 1, image.Height);

        var i = 0;
        Assert.AreEqual(new Rgba32(0x00, 0x00, 0x00), image[0, 6 * i++], $"0x{i}: #000000");
        Assert.AreEqual(new Rgba32(0x00, 0x00, 0xFF), image[0, 6 * i++], $"0x{i}: #0000FF");
        Assert.AreEqual(new Rgba32(0x80, 0x00, 0xFF), image[0, 6 * i++], $"0x{i}: #8000FF");
        Assert.AreEqual(new Rgba32(0xFF, 0x00, 0xFF), image[0, 6 * i++], $"0x{i}: #FF00FF");
        Assert.AreEqual(new Rgba32(0xFF, 0x00, 0x80), image[0, 6 * i++], $"0x{i}: #FF0080");
        Assert.AreEqual(new Rgba32(0xFF, 0x00, 0x00), image[0, 6 * i++], $"0x{i}: #FF0000");
        Assert.AreEqual(new Rgba32(0xFF, 0x80, 0x00), image[0, 6 * i++], $"0x{i}: #FF8000");
        Assert.AreEqual(new Rgba32(0xFF, 0xFF, 0x00), image[0, 6 * i++], $"0x{i}: #FFFF00");
        Assert.AreEqual(new Rgba32(0x80, 0xFF, 0x00), image[0, 6 * i++], $"0x{i}: #80FF00");
        Assert.AreEqual(new Rgba32(0x00, 0xFF, 0x00), image[0, 6 * i++], $"0x{i}: #00FF00");
        Assert.AreEqual(new Rgba32(0x00, 0xFF, 0x80), image[0, 6 * i++], $"0x{i}: #00FF80");
        Assert.AreEqual(new Rgba32(0x00, 0xFF, 0xFF), image[0, 6 * i++], $"0x{i}: #00FFFF");
        Assert.AreEqual(new Rgba32(0x00, 0x80, 0xFF), image[0, 6 * i++], $"0x{i}: #0080FF");
        Assert.AreEqual(new Rgba32(0xFF, 0xFF, 0xFF), image[0, 6 * i++], $"0x{i}: #FFFFFF");
        Assert.AreEqual(new Rgba32(0x00, 0x00, 0x00), image[5, 6 * i], $"5x{i}: #000000");
    }

    [TestMethod("[Decode] HLS color")]
    public void TestMethod2()
    {
        var sixelString = $"""
            {ESC}P8;1;q"1;1;100;72
            #0;1;0;50;100
            #1;1;30;50;100
            #2;1;60;50;100
            #3;1;90;50;100
            #4;1;120;50;100
            #5;1;150;50;100
            #6;1;180;50;100
            #7;1;210;50;100
            #8;1;240;50;100
            #9;1;270;50;100
            #10;1;300;50;100
            #11;1;330;50;100
            #0!100~-
            #1!100~-
            #2!100~-
            #3!100~-
            #4!100~-
            #5!100~-
            #6!100~-
            #7!100~-
            #8!100~-
            #9!100~-
            #10!100~-
            #11!100~
            {ESC}\
            """;
        Console.WriteLine(sixelString);
        using var image = Sixel.Decode(sixelString.Trim());

        Assert.AreEqual(100, image.Width);
        Assert.AreEqual(12 * 6, image.Height);

        var i = 0;
        Assert.AreEqual(new Rgba32(0x00, 0x00, 0xFF), image[0, 6 * i++], $"{i}: #0000FF");
        Assert.AreEqual(new Rgba32(0x80, 0x00, 0xFF), image[0, 6 * i++], $"{i}: #8000FF");
        Assert.AreEqual(new Rgba32(0xFF, 0x00, 0xFF), image[0, 6 * i++], $"{i}: #FF00FF");
        Assert.AreEqual(new Rgba32(0xFF, 0x00, 0x80), image[0, 6 * i++], $"{i}: #FF0080");
        Assert.AreEqual(new Rgba32(0xFF, 0x00, 0x00), image[0, 6 * i++], $"{i}: #FF0000");
        Assert.AreEqual(new Rgba32(0xFF, 0x80, 0x00), image[0, 6 * i++], $"{i}: #FF8000");
        Assert.AreEqual(new Rgba32(0xFF, 0xFF, 0x00), image[0, 6 * i++], $"{i}: #FFFF00");
        Assert.AreEqual(new Rgba32(0x80, 0xFF, 0x00), image[0, 6 * i++], $"{i}: #80FF00");
        Assert.AreEqual(new Rgba32(0x00, 0xFF, 0x00), image[0, 6 * i++], $"{i}: #00FF00");
        Assert.AreEqual(new Rgba32(0x00, 0xFF, 0x80), image[0, 6 * i++], $"{i}: #00FF80");
        Assert.AreEqual(new Rgba32(0x00, 0xFF, 0xFF), image[0, 6 * i++], $"{i}: #00FFFF");
        Assert.AreEqual(new Rgba32(0x00, 0x80, 0xFF), image[0, 6 * i++], $"{i}: #0080FF");
    }
}
