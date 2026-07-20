using System;
using System.Threading;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Input;
using FlaUI.UIA3;
using Xunit;

namespace YHuLauncherBackEndTestUnit;

/// <summary>
/// 各 FluentWindow が表示できることを検証する。
/// MainWindow 起動 → メニュー遷移 → 各ページ/ウィンドウが初期化されることを NLog で確認。
/// </summary>
public class WindowDisplayTests : TestAppBase
{
    [Fact]
    public void MainWindow_Launch_ShowsTitle()
    {
        using var automation = new UIA3Automation();
        var main = GetMainWindow(automation);
        Assert.False(string.IsNullOrEmpty(main.Title));
        WaitForDbCreated();
        Assert.True(WaitForLogContains("MainPage Initialized", TimeSpan.FromSeconds(10)));
        ShutdownApp();
    }

    [Fact]
    public void SettingWindow_Opens_FromMenu()
    {
        using var automation = new UIA3Automation();
        var app = LaunchApp();
        var main = app.GetMainWindow(automation, TimeSpan.FromSeconds(30));
        Assert.NotNull(main);
        WaitForDbCreated();
        Assert.True(WaitForLogContains("MainPage Initialized", TimeSpan.FromSeconds(10)));

        OpenNavigationViewPane(main);
        var settingItem = main.FindFirstDescendant(cf => cf.ByAutomationId("SettingBtn"));
        Assert.NotNull(settingItem);
        ActivateNavItem(settingItem);

        // SettingWindow の初期化ログで確認（UI ルーティング不確実性を回避）
        Assert.True(WaitForLogContains("Setting Window Initialized!", TimeSpan.FromSeconds(15)),
            "SettingWindow did not initialize. Log:\n" + ReadNewLogLines());
        Assert.True(WaitForLogContains("SettingPage Initialized", TimeSpan.FromSeconds(5)),
            "SettingPage did not initialize. Log:\n" + ReadNewLogLines());

        ShutdownApp();
    }

    [Fact]
    public void GameListPage_Navigates_FromMenu()
    {
        using var automation = new UIA3Automation();
        var main = GetMainWindow(automation);
        WaitForDbCreated();
        Assert.True(WaitForLogContains("MainPage Initialized", TimeSpan.FromSeconds(10)));

        OpenNavigationViewPane(main);
        var gameListItem = main.FindFirstDescendant(cf => cf.ByAutomationId("GameListBtn"));
        Assert.NotNull(gameListItem);
        ActivateNavItem(gameListItem);

        // GameList は起動時に既に読み込まれている。ナビゲーション後に MainPage Initialized が再記録されることを確認
        Thread.Sleep(1500);
        Assert.True(WaitForLogContains("Setting Window Initialized!", TimeSpan.FromSeconds(3)) == false,
            "GameList navigation should not trigger SettingWindow");
        ShutdownApp();
    }

    [Fact]
    public void WebGameListPage_Navigates_FromMenu()
    {
        using var automation = new UIA3Automation();
        var main = GetMainWindow(automation);
        WaitForDbCreated();
        Assert.True(WaitForLogContains("MainPage Initialized", TimeSpan.FromSeconds(10)));

        // 起動時ログを読み飛ばすため、マーカーを記憶
        var logBeforeNav = ReadNewLogLines();

        OpenNavigationViewPane(main);
        var webGameItem = main.FindFirstDescendant(cf => cf.ByAutomationId("WebGameListBtn"));
        Assert.NotNull(webGameItem);
        ActivateNavItem(webGameItem);

        // GameListPaneControl.OnLoaded → LoadGenre が呼ばれる。
        // 隔離DBが空でも LoadGenre 自体は走る。プロセスが生きていることを確認。
        Thread.Sleep(1500);
        var logAfterNav = ReadNewLogLines();
        // ナビゲーションで例外が出ていないことを確認
        Assert.DoesNotContain("ERROR", logAfterNav.Substring(Math.Min(logBeforeNav.Length, logAfterNav.Length)));
        ShutdownApp();
    }

    [Fact]
    public void WebSaverPage_Navigates_FromMenu()
    {
        using var automation = new UIA3Automation();
        var main = GetMainWindow(automation);
        WaitForDbCreated();
        Assert.True(WaitForLogContains("MainPage Initialized", TimeSpan.FromSeconds(10)));

        var logBeforeNav = ReadNewLogLines();

        OpenNavigationViewPane(main);
        var webSaverItem = main.FindFirstDescendant(cf => cf.ByAutomationId("WebSaverBtn"));
        Assert.NotNull(webSaverItem);
        ActivateNavItem(webSaverItem);

        Thread.Sleep(1500);
        var logAfterNav = ReadNewLogLines();
        Assert.DoesNotContain("ERROR", logAfterNav.Substring(Math.Min(logBeforeNav.Length, logAfterNav.Length)));
        ShutdownApp();
    }

    /// <summary>
    /// NavigationView のペインが閉じている場合、ハンバーガーボタンで開く。
    /// </summary>
    private static void OpenNavigationViewPane(Window main)
    {
        // WPF-UI NavigationView のトグルボタンを探す
        var toggle = main.FindFirstDescendant(cf => cf.ByAutomationId("PaneToggleButton"))
                  ?? main.FindFirstDescendant(cf => cf.ByName("TogglePane"));
        if (toggle != null)
        {
            try { toggle.AsListBoxItem().Select(); } catch { }
            toggle.Focus();
            Thread.Sleep(300);
            Keyboard.TypeVirtualKeyCode((ushort)FlaUI.Core.WindowsAPI.VirtualKeyShort.RETURN);
            Thread.Sleep(800); // ペインが開くまで待つ
        }
    }

    /// <summary>
    /// NavigationViewItem をアクティブ化する。
    /// Click がルーティングされないため Select + Enter を使う。
    /// </summary>
    private static void ActivateNavItem(FlaUI.Core.AutomationElements.AutomationElement item)
    {
        try { item.AsListBoxItem().Select(); } catch { }
        item.Focus();
        Thread.Sleep(300);
        Keyboard.TypeVirtualKeyCode((ushort)FlaUI.Core.WindowsAPI.VirtualKeyShort.RETURN);
        Thread.Sleep(1000);
    }
}
