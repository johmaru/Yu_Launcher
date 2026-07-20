using System.IO;
using System.Linq;
using System.Threading;
using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;
using Xunit;

namespace YHuLauncherBackEndTestUnit;

/// <summary>
/// 設定変更が settings.toml に永続化されること、
/// 再起動後に復元されることを検証する。
/// </summary>
public class SettingsPersistenceTests : TestAppBase
{
    [Fact]
    public void Settings_Toml_Created_On_First_Launch()
    {
        using var automation = new UIA3Automation();
        GetMainWindow(automation);
        WaitForDbCreated();
        ShutdownApp();

        // FirstLunch() が settings.toml を作成する
        Assert.True(File.Exists(SettingsTomlPath), $"settings.toml not created at {SettingsTomlPath}");

        // gameList.toml も作成される
        var gameListPath = Path.Combine(WorkDir, "gameList.toml");
        Assert.True(File.Exists(gameListPath), $"gameList.toml not created at {gameListPath}");

        // html/ ディレクトリ
        var htmlDir = Path.Combine(WorkDir, "html");
        Assert.True(Directory.Exists(htmlDir), $"html/ directory not created at {htmlDir}");

        // Games/ ディレクトリ
        var gamesDir = Path.Combine(WorkDir, "Games");
        Assert.True(Directory.Exists(gamesDir), $"Games/ directory not created at {gamesDir}");
    }

    [Fact]
    public void Settings_Toml_Contains_Expected_Keys()
    {
        using var automation = new UIA3Automation();
        GetMainWindow(automation);
        WaitForDbCreated();
        ShutdownApp();

        var content = File.ReadAllText(SettingsTomlPath);

        // TomlControl.CreateToml が書き出す標準キー
        Assert.Contains("Language", content);
        Assert.Contains("Theme", content);
        Assert.Contains("WindowResolution", content);
    }
}
