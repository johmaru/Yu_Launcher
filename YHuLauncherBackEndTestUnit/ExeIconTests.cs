using System.Runtime.ExceptionServices;
using System.Threading;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using YuLauncher.Core.lib;

namespace YHuLauncherBackEndTestUnit;

public class ExeIconTests
{
    [Fact]
    public void GetImage_ShowsShellIconForExecutablesAndReturnsNullForMissingFile()
    {
        var directory = Directory.CreateTempSubdirectory();
        try
        {
            var noIconExe = Path.Combine(directory.FullName, "NoIcon.exe");
            File.Copy(typeof(ExeIconTests).Assembly.Location, noIconExe);

            Exception? failure = null;
            var thread = new Thread(() =>
            {
                try
                {
                    AssertVisibleIcon(noIconExe);
                    AssertVisibleIcon(Path.Combine(Environment.SystemDirectory, "cmd.exe"));
                    Assert.Null(GameButton.GetImage(new JsonControl.ApplicationJsonData
                    {
                        FileExtension = "exe",
                        FilePath = Path.Combine(directory.FullName, "Missing.exe")
                    }));
                }
                catch (Exception ex)
                {
                    failure = ex;
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
            if (failure != null) ExceptionDispatchInfo.Capture(failure).Throw();
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    private static void AssertVisibleIcon(string path)
    {
        BitmapSource? image = GameButton.GetImage(new JsonControl.ApplicationJsonData
        {
            FileExtension = "exe",
            FilePath = path
        });
        Assert.NotNull(image);

        var bgra = new FormatConvertedBitmap(image, PixelFormats.Bgra32, null, 0);
        var pixels = new byte[bgra.PixelWidth * bgra.PixelHeight * 4];
        bgra.CopyPixels(pixels, bgra.PixelWidth * 4, 0);
        Assert.True(pixels.Where((_, index) => index % 4 == 3).Any(alpha => alpha > 0),
            $"Icon has no visible pixels: {path}");
    }
}
