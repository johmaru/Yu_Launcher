using System.Reflection;
using System.Runtime.ExceptionServices;
using YuLauncher;

namespace YHuLauncherBackEndTestUnit;

public class UpdateBackupTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Backup_MissingOptionalFoldersPublishesExistingData(bool withFolders)
    {
        WithCurrent(root =>
        {
            File.WriteAllText("settings.toml", "settings-before");
            if (withFolders)
            {
                Directory.CreateDirectory("Games/nested");
                Directory.CreateDirectory("html");
                File.WriteAllText("Games/nested/game.txt", "game-data");
                File.WriteAllText("html/page.html", "page-data");
            }
            Invoke("YuLauncher.App", "temp_file");
            string backup = Path.Combine(root, "Temp");
            Assert.Equal("settings-before", File.ReadAllText(Path.Combine(backup, "settings.toml")));
            Assert.False(Directory.Exists(Path.Combine(backup, "YuLauncher.exe.WebView2")));
            if (withFolders)
            {
                Assert.Equal("game-data", File.ReadAllText(Path.Combine(backup, "Games/nested/game.txt")));
                Assert.Equal("page-data", File.ReadAllText(Path.Combine(backup, "html/page.html")));
            }
            else
            {
                Assert.False(Directory.Exists(Path.Combine(backup, "Games")));
                Assert.False(Directory.Exists(Path.Combine(backup, "html")));
            }
        });
    }

    [Fact]
    public void Backup_FailureDoesNotPublishOrDiscardExistingData()
    {
        WithCurrent(root =>
        {
            File.WriteAllText("settings.toml", "settings-before");
            Directory.CreateDirectory("Games");
            File.WriteAllText("Games/game.txt", "game-data");
            using (new FileStream("settings.toml", FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
                Assert.Throws<IOException>(() => Invoke("YuLauncher.App", "temp_file"));
            }
            Assert.False(Directory.Exists(Path.Combine(root, "Temp")));
            Assert.Equal("settings-before", File.ReadAllText("settings.toml"));
            string pending = Directory.EnumerateDirectories(root, "Temp.pending.*").Single();
            Assert.Equal("game-data", File.ReadAllText(Path.Combine(pending, "Games/game.txt")));
        });
        WithCurrent(root =>
        {
            File.WriteAllText("settings.toml", "current-settings");
            string backup = Directory.CreateDirectory(Path.Combine(root, "Temp")).FullName;
            File.WriteAllText(Path.Combine(backup, "settings.toml"), "old-backup");
            Assert.Throws<IOException>(() => Invoke("YuLauncher.App", "temp_file"));
            Assert.Equal("old-backup", File.ReadAllText(Path.Combine(backup, "settings.toml")));
            Assert.Equal("current-settings", File.ReadAllText("settings.toml"));
        });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Restore_PartialBackupMergesWithoutOverwritingCurrentData(bool hasCurrentSettings)
    {
        WithCurrent(root =>
        {
            string backup = Directory.CreateDirectory(Path.Combine(root, "Temp")).FullName;
            Directory.CreateDirectory(Path.Combine(backup, "Games/nested"));
            File.WriteAllText(Path.Combine(backup, "Games/nested/existing.txt"), "old-game");
            File.WriteAllText(Path.Combine(backup, "Games/nested/missing.txt"), "saved-game");
            File.WriteAllText(Path.Combine(backup, "settings.toml"), "old-settings");
            Directory.CreateDirectory("Games/nested");
            File.WriteAllText("Games/nested/existing.txt", "current-game");
            if (hasCurrentSettings) File.WriteAllText("settings.toml", "current-settings");
            Invoke("YuLauncher.Program", "Temp");
            Assert.Equal("current-game", File.ReadAllText("Games/nested/existing.txt"));
            Assert.Equal("saved-game", File.ReadAllText("Games/nested/missing.txt"));
            Assert.Equal(hasCurrentSettings ? "current-settings" : "old-settings", File.ReadAllText("settings.toml"));
            Assert.False(Directory.Exists(backup));
        });
    }

    [Fact]
    public void Restore_ReadFailurePreservesBackupAndAllowsRetry()
    {
        WithCurrent(root =>
        {
            string backup = Directory.CreateDirectory(Path.Combine(root, "Temp")).FullName;
            Directory.CreateDirectory(Path.Combine(backup, "Games"));
            Directory.CreateDirectory(Path.Combine(backup, "html"));
            string savedGame = Path.Combine(backup, "Games/game.txt");
            string savedPage = Path.Combine(backup, "html/page.html");
            File.WriteAllText(savedGame, "game-data");
            File.WriteAllText(savedPage, "page-data");
            using (new FileStream(savedPage, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
                Assert.Throws<IOException>(() => Invoke("YuLauncher.Program", "Temp"));
                Assert.Equal("game-data", File.ReadAllText(savedGame));
                Assert.Equal("game-data", File.ReadAllText("Games/game.txt"));
            }
            Assert.Equal("page-data", File.ReadAllText(savedPage));
            Invoke("YuLauncher.Program", "Temp");
            Assert.Equal("game-data", File.ReadAllText("Games/game.txt"));
            Assert.Equal("page-data", File.ReadAllText("html/page.html"));
            Assert.False(Directory.Exists(backup));
        });
    }

    [Fact]
    public void Restore_EmptyBackupIsRemovedAndUnknownDataIsRetained()
    {
        WithCurrent(root =>
        {
            string backup = Directory.CreateDirectory(Path.Combine(root, "Temp")).FullName;
            Invoke("YuLauncher.Program", "Temp");
            Assert.False(Directory.Exists(backup));
        });
        WithCurrent(root =>
        {
            string backup = Directory.CreateDirectory(Path.Combine(root, "Temp")).FullName;
            string unknown = Path.Combine(backup, "keep.bin");
            File.WriteAllText(unknown, "unrecognized-user-data");
            Invoke("YuLauncher.Program", "Temp");
            Assert.Equal("unrecognized-user-data", File.ReadAllText(unknown));
        });
    }

    private static void WithCurrent(Action<string> action)
    {
        var root = Directory.CreateTempSubdirectory("YuLauncherBackupTests_");
        string original = Directory.GetCurrentDirectory();
        try
        {
            string current = Directory.CreateDirectory(Path.Combine(root.FullName, "current")).FullName;
            Directory.SetCurrentDirectory(current);
            action(root.FullName);
        }
        finally
        {
            Directory.SetCurrentDirectory(original);
            root.Delete(recursive: true);
        }
    }

    private static void Invoke(string typeName, string methodName)
    {
        try
        {
            var type = typeof(App).Assembly.GetType(typeName, throwOnError: true)!;
            var method = type.GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic)!;
            object? result = method.Invoke(null, null);
            if (result is ValueTask task) task.GetAwaiter().GetResult();
        }
        catch (TargetInvocationException error) when (error.InnerException != null)
        {
            ExceptionDispatchInfo.Capture(error.InnerException).Throw();
        }
    }
}
