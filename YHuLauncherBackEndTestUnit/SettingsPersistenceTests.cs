using System.IO;
using System.Linq;
using System.Threading;
using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;
using YuLauncher.Core.lib;
using Xunit;

namespace YHuLauncherBackEndTestUnit;

/// <summary>
/// 設定変更が settings.toml に永続化されること、
/// 再起動後に復元されることを検証する。
/// </summary>
public class SettingsPersistenceTests : TestAppBase
{
    [Fact]
    public void Settings_Toml_Created_With_Default_Values_On_First_Launch()
    {
        using var automation = new UIA3Automation();
        GetMainWindow(automation);
        WaitForDbCreated();
        ShutdownApp();

        // FirstLunch() が settings.toml を作成する
        Assert.True(File.Exists(SettingsTomlPath), $"settings.toml not created at {SettingsTomlPath}");

        // gameList.toml も作成される
        var gameListPath = Path.Combine(WorkDir, "gameList.toml");
        Assert.True(File.Exists(gameListPath));

        // html/ と Games/ ディレクトリ
        Assert.True(Directory.Exists(Path.Combine(WorkDir, "html")));
        Assert.True(Directory.Exists(Path.Combine(WorkDir, "Games")));

        // デフォルト値を検証（部分一致ではなく完全一致）
        Assert.Equal("en", TomlControl.GetTomlString(SettingsTomlPath, "Language"));
        Assert.Equal("Dark", TomlControl.GetTomlString(SettingsTomlPath, "Theme"));
        Assert.Equal("false", TomlControl.GetTomlString(SettingsTomlPath, "FullScreen"));
        Assert.Equal("false", TomlControl.GetTomlString(SettingsTomlPath, "AutoUpdate"));
        Assert.Equal("800", TomlControl.GetTomlString(SettingsTomlPath, "WindowResolution", "Width"));
        Assert.Equal("400", TomlControl.GetTomlString(SettingsTomlPath, "WindowResolution", "Height"));
    }

    [Fact]
    public void Settings_Change_Persists_After_Restart()
    {
        // 1回目の起動: settings.toml 作成
        using (var automation = new UIA3Automation())
        {
            GetMainWindow(automation);
            WaitForDbCreated();
            ShutdownApp();
        }

        Assert.True(File.Exists(SettingsTomlPath));

        // テストプロセスから設定を変更（SettingWindow のUI操作と同等）
        TomlControl.EditToml(SettingsTomlPath, "Theme", "Light");
        TomlControl.EditToml(SettingsTomlPath, "Language", "ja");
        TomlControl.EditToml(SettingsTomlPath, "WindowResolution", "Width", "1024");
        TomlControl.EditToml(SettingsTomlPath, "WindowResolution", "Height", "768");

        // 変更が即座にファイルに反映されていることを確認
        Assert.Equal("Light", TomlControl.GetTomlString(SettingsTomlPath, "Theme"));
        Assert.Equal("ja", TomlControl.GetTomlString(SettingsTomlPath, "Language"));
        Assert.Equal("1024", TomlControl.GetTomlString(SettingsTomlPath, "WindowResolution", "Width"));
        Assert.Equal("768", TomlControl.GetTomlString(SettingsTomlPath, "WindowResolution", "Height"));

        // 2回目の起動: FirstLunch() は settings.toml が存在するので上書きしない
        using (var automation = new UIA3Automation())
        {
            GetMainWindow(automation);
            WaitForDbCreated();
            // MainWindow が設定値を読み込んで起動することを確認（例外が出なければOK）
            ShutdownApp();
        }

        // 再起動後も設定が保持されていることを検証
        Assert.Equal("Light", TomlControl.GetTomlString(SettingsTomlPath, "Theme"));
        Assert.Equal("ja", TomlControl.GetTomlString(SettingsTomlPath, "Language"));
        Assert.Equal("1024", TomlControl.GetTomlString(SettingsTomlPath, "WindowResolution", "Width"));
        Assert.Equal("768", TomlControl.GetTomlString(SettingsTomlPath, "WindowResolution", "Height"));
    }
}
