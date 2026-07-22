using System.IO;
using System.Linq;
using System.Threading;
using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;
using YuLauncher.Core.lib;
using Xunit;

namespace YHuLauncherBackEndTestUnit;

/// <summary>
/// 言語設定に応じてUIがローカライズされることを検証する。
/// en: "Game List", ja: "ゲーム一覧" 等の実際の表示文字列で確認。
/// </summary>
public class LocalizationTests : TestAppBase
{
    [Fact]
    public void English_Language_Shows_English_UI()
    {
        // 1回目の起動で settings.toml 作成
        using (var automation = new UIA3Automation())
        {
            GetMainWindow(automation);
            WaitForDbCreated();
            ShutdownApp();
        }

        // 言語を明示的に en に設定
        TomlControl.EditToml(SettingsTomlPath, "Language", "en");

        using var automation2 = new UIA3Automation();
        var main = GetMainWindow(automation2);
        WaitForDbCreated();
        Assert.True(WaitForLogContains("MainPage Initialized", System.TimeSpan.FromSeconds(10)));

        // NavigationView を開いてメニューアイテムを確認
        OpenNavigationViewPane(main);
        Thread.Sleep(500);

        // 英語のメニューテキストが表示されることを確認
        var gameListItem = main.FindFirstDescendant(cf => cf.ByName("Game List"));
        Assert.NotNull(gameListItem);

        var settingItem = main.FindFirstDescendant(cf => cf.ByName("Setting"));
        Assert.NotNull(settingItem);

        ShutdownApp();
    }

    [Fact]
    public void Japanese_Language_Shows_Japanese_UI()
    {
        // 1回目の起動で settings.toml 作成
        using (var automation = new UIA3Automation())
        {
            GetMainWindow(automation);
            WaitForDbCreated();
            ShutdownApp();
        }

        // 言語を明示的に ja に設定
        TomlControl.EditToml(SettingsTomlPath, "Language", "ja");

        using var automation2 = new UIA3Automation();
        var main = GetMainWindow(automation2);
        WaitForDbCreated();
        Assert.True(WaitForLogContains("MainPage Initialized", System.TimeSpan.FromSeconds(10)));

        // NavigationView を開いてメニューアイテムを確認
        OpenNavigationViewPane(main);
        Thread.Sleep(500);

        // 日本語のメニューテキストが表示されることを確認
        var gameListItem = main.FindFirstDescendant(cf => cf.ByName("ゲーム一覧"));
        Assert.NotNull(gameListItem);

        // 生のリソースキーが表示されていないことを確認
        var rawKey = main.FindFirstDescendant(cf => cf.ByName("GameList"));
        Assert.Null(rawKey);

        ShutdownApp();
    }

    [Fact]
    public void MainWindow_Title_Is_Not_RawResourceKey()
    {
        using var automation = new UIA3Automation();
        var main = GetMainWindow(automation);
        WaitForDbCreated();

        var title = main.Title ?? "";
        Assert.False(string.IsNullOrEmpty(title), "MainWindow title is empty");

        // MainWindow.xaml は Title="YuLancher" ハードコード（既知の仕様）
        // ローカライズされていないが、生のリソースキーではないことを確認
        Assert.DoesNotContain("CtxHeader", title);
        Assert.DoesNotContain("WindowTitle", title);

        ShutdownApp();
    }
}
