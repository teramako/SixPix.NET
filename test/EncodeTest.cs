using SixPix;

namespace test;

[TestClass]
public class EncodeTest
{
    const char ESC = '\x1b';
    const string SixelStart = "P7;1;q";
    const string SixelEnd = "\\";

    const string PROJ_DIR = "../../..";

    private static void CompareData(string expect, string actual)
    {
        var expectLines = expect.Replace(ESC.ToString(), "^[").Split('#');
        var sixelLines = actual.Replace(ESC.ToString(), "^[").Split('#');
        for (var i = 0; i < expectLines.Length; i++)
        {
            Assert.AreEqual<string>(expectLines[i], sixelLines[i]);
        }
    }

    [TestMethod("[Encode] test_1.png")]
    public void TestMethod1()
    {
        var resultFileInfo = new FileInfo(PROJ_DIR + "/data/test_1_sixel.out");
        var result = File.ReadAllText(resultFileInfo.FullName).Trim();

        var imageFileInfo = new FileInfo(PROJ_DIR + "/data/test_1.png");
        Assert.IsTrue(imageFileInfo.Exists);
        using var fs = imageFileInfo.OpenRead();

        var sixel = Sixel.Encode(fs);

        Assert.IsTrue(sixel.StartsWith(ESC + SixelStart));
        Assert.IsTrue(sixel.EndsWith(ESC + SixelEnd));

        CompareData(result, sixel.ToString());
    }

    [TestMethod("[Encode] test_2.png")]
    public void TestMethod2()
    {
        var resultFileInfo = new FileInfo(PROJ_DIR + "/data/test_2_sixel.out");
        var result = File.ReadAllText(resultFileInfo.FullName).Trim();

        var imageFileInfo = new FileInfo(PROJ_DIR + "/data/test_2.png");
        Assert.IsTrue(imageFileInfo.Exists);
        using var fs = imageFileInfo.OpenRead();

        var sixel = Sixel.Encode(fs);

        Assert.IsTrue(sixel.StartsWith(ESC + SixelStart));
        Assert.IsTrue(sixel.EndsWith(ESC + SixelEnd));

        CompareData(result, sixel.ToString());
    }
}
