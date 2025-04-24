using SixPix;

namespace test;

[TestClass]
public class EncodeTest
{
    private const string DATA_DIR = "../../../data/";

    private static void CompareData(string expect, string actual)
    {
        var expectLines = expect.Replace(Sixel.ESC.ToString(), "^[").Split('#');
        var sixelLines = actual.Replace(Sixel.ESC.ToString(), "^[").Split('#');
        for (var i = 0; i < expectLines.Length; i++)
        {
            Assert.AreEqual<string>(expectLines[i], sixelLines[i]);
        }
    }

    [TestMethod("[Encode] test_1.png (transparent)")]
    public void TestMethod1Transp()
    {
        var result1FileInfo = new FileInfo(DATA_DIR + "test_1t.six");
        var result1 = File.ReadAllText(result1FileInfo.FullName).Trim();

        var image1FileInfo = new FileInfo(DATA_DIR + "test_1.png");
        Assert.IsTrue(image1FileInfo.Exists);
        using var fs = image1FileInfo.OpenRead();

        var sixel = Sixel.Encode(fs);

        Assert.IsTrue(sixel.StartsWith(Sixel.ESC + Sixel.TranspStart));
        Assert.IsTrue(sixel.EndsWith(Sixel.ESC + Sixel.End));

        CompareData(result1, sixel.ToString());
    }

    [TestMethod("[Encode] test_2.png (no transparency)")]
    public void TestMethod2Opaque()
    {
        var result2oFileInfo = new FileInfo(DATA_DIR + "test_2o.six");
        var result2o = File.ReadAllText(result2oFileInfo.FullName).Trim();

        var image2FileInfo = new FileInfo(DATA_DIR + "test_2.png");
        Assert.IsTrue(image2FileInfo.Exists);
        using var fs = image2FileInfo.OpenRead();

        var sixel = Sixel.Encode(fs, transp: Transparency.None);

        Assert.IsTrue(sixel.StartsWith(Sixel.ESC + Sixel.OpaqueStart));
        Assert.IsTrue(sixel.EndsWith(Sixel.ESC + Sixel.End));

        CompareData(result2o, sixel.ToString());
    }

    [TestMethod("[Encode] test_2.png (top-left transparency)")]
    public void TestMethod2TL()
    {
        var result2TLFileInfo = new FileInfo(DATA_DIR + "test_2tl.six");
        var result2TL = File.ReadAllText(result2TLFileInfo.FullName).Trim();

        var image2FileInfo = new FileInfo(DATA_DIR + "test_2.png");
        Assert.IsTrue(image2FileInfo.Exists);
        using var fs = image2FileInfo.OpenRead();

        var sixel = Sixel.Encode(fs, transp: Transparency.TopLeft);

        Assert.IsTrue(sixel.StartsWith(Sixel.ESC + Sixel.TranspStart));
        Assert.IsTrue(sixel.EndsWith(Sixel.ESC + Sixel.End));

        CompareData(result2TL, sixel.ToString());
    }

    [TestMethod("[Encode] test_anim.png")]
    public void TestMethodAnim()
    {
        var result1FileInfo = new FileInfo(DATA_DIR + "test_1t.six");
        var result2FileInfo = new FileInfo(DATA_DIR + "test_2t.six");
        var result1 = File.ReadAllText(result1FileInfo.FullName).Trim();
        var result2 = File.ReadAllText(result2FileInfo.FullName).Trim();

        var imageAFileInfo = new FileInfo(DATA_DIR + "test_anim.png");
        Assert.IsTrue(imageAFileInfo.Exists);
        using var fs = imageAFileInfo.OpenRead();

        for (int i = 0; i < 4; i++)
        {
            var sixel = Sixel.Encode(fs, frame: i);
            fs.Seek(0, 0);
            Assert.IsTrue(sixel.StartsWith(Sixel.ESC + Sixel.TranspStart));
            Assert.IsTrue(sixel.EndsWith(Sixel.ESC + Sixel.End));
            if (i % 2 == 0)
                CompareData(result1, sixel.ToString());
            else
                CompareData(result2, sixel.ToString());
        }
    }
}
