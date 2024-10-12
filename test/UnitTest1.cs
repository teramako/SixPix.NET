using SixPix;

namespace test;

[TestClass]
public class UnitTest1
{
    const string SixelStart = "\x1bPq";
    const string SixelEnd = "\x1b\\";

    const string PROJ_DIR = "../../..";

    [TestMethod("test_1.png")]
    public void TestMethod1()
    {
        var resultFileInfo = new FileInfo(PROJ_DIR + "/data/test_1_sixel.out");
        var result = File.ReadAllText(resultFileInfo.FullName).Trim();

        var imageFileInfo = new FileInfo(PROJ_DIR + "/data/test_1.png");
        Assert.IsTrue(imageFileInfo.Exists);
        using var fs = imageFileInfo.OpenRead();

        var sixel = Sixel.Encode(fs);

        Assert.IsTrue(sixel.StartsWith(SixelStart));
        Assert.IsTrue(sixel.EndsWith(SixelEnd));

        Assert.AreEqual<string>(result, sixel.ToString());
    }

    [TestMethod("test_2.png")]
    public void TestMethod2()
    {
        var resultFileInfo = new FileInfo(PROJ_DIR + "/data/test_2_sixel.out");
        var result = File.ReadAllText(resultFileInfo.FullName).Trim();

        var imageFileInfo = new FileInfo(PROJ_DIR + "/data/test_2.png");
        Assert.IsTrue(imageFileInfo.Exists);
        using var fs = imageFileInfo.OpenRead();

        var sixel = Sixel.Encode(fs);

        Assert.IsTrue(sixel.StartsWith(SixelStart));
        Assert.IsTrue(sixel.EndsWith(SixelEnd));

        Assert.AreEqual<string>(result, sixel.ToString());
    }
}
