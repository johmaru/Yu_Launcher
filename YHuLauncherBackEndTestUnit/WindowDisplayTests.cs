using System;
using System.Linq;
using System.Threading;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.UIA3;
using Xunit;

namespace YHuLauncherBackEndTestUnit;

/// <summary>
/// ナビゲーション検証: 各ページの FileExtensionFilter が実際に機能することを
/// シードデータの表示件数で検証する。
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

        Assert.True(WaitForLogContains("Setting Window Initialized!", TimeSpan.FromSeconds(15)),
            "SettingWindow did not initialize. Log:\n" + ReadNewLogLines());
        Assert.True(WaitForLogContains("SettingPage Initialized", TimeSpan.FromSeconds(5)),
            "SettingPage did not initialize. Log:\n" + ReadNewLogLines());

        ShutdownApp();
    }

    [Fact]
    public void GameListPage_Shows_NonWebGame_Items()
    {
        SeedDatabase(
            TestDataFactory.CreateExeGame(),
            TestDataFactory.CreateWebGame(),
            TestDataFactory.CreateWebSaver()
        );

        using var automation = new UIA3Automation();
        var main = GetMainWindow(automation);
        WaitForDbCreated();
        WaitForLogContains("MainPage Initialized", TimeSpan.FromSeconds(10));

        // MainPage.Init() が起動時に GameList.xaml (FileExtensionFilter=All) を読み込む
        // All フィルタ: data.FileExtension != "WebGame" → exe と websaver の2件
        var gameListBox = WaitForDescendant(main, "GameListBox", TimeSpan.FromSeconds(15));
        Assert.NotNull(gameListBox);

        var names = GetListBoxItemNames(gameListBox.AsListBox());
        Assert.Equal(2, names.Count);
        Assert.Contains("TestExeGame", names);
        Assert.Contains("TestWebSaver", names);
        Assert.DoesNotContain("TestWebGame", names);

        ShutdownApp();
    }

    [Fact]
    public void WebGameListPage_Shows_Only_WebGame_Items()
    {
        SeedDatabase(
            TestDataFactory.CreateExeGame(),
            TestDataFactory.CreateWebGame(),
            TestDataFactory.CreateWebSaver()
        );

        using var automation = new UIA3Automation();
        var main = GetMainWindow(automation);
        WaitForDbCreated();
        WaitForLogContains("MainPage Initialized", TimeSpan.FromSeconds(10));

        OpenNavigationViewPane(main);
        var webGameItem = main.FindFirstDescendant(cf => cf.ByAutomationId("WebGameListBtn"));
        Assert.NotNull(webGameItem);
        ActivateNavItem(webGameItem);

        var gameListBox = WaitForDescendant(main, "GameListBox", TimeSpan.FromSeconds(15));
        Assert.NotNull(gameListBox);

        var names = GetListBoxItemNames(gameListBox.AsListBox());
        Assert.Single(names);
        Assert.Contains("TestWebGame", names);
        Assert.DoesNotContain("TestExeGame", names);
        Assert.DoesNotContain("TestWebSaver", names);

        ShutdownApp();
    }

    [Fact]
    public void WebSaverPage_Shows_Only_WebSaver_Items()
    {
        SeedDatabase(
            TestDataFactory.CreateExeGame(),
            TestDataFactory.CreateWebGame(),
            TestDataFactory.CreateWebSaver()
        );

        using var automation = new UIA3Automation();
        var main = GetMainWindow(automation);
        WaitForDbCreated();
        WaitForLogContains("MainPage Initialized", TimeSpan.FromSeconds(10));

        OpenNavigationViewPane(main);
        var webSaverItem = main.FindFirstDescendant(cf => cf.ByAutomationId("WebSaverBtn"));
        Assert.NotNull(webSaverItem);
        ActivateNavItem(webSaverItem);

        var gameListBox = WaitForDescendant(main, "GameListBox", TimeSpan.FromSeconds(15));
        Assert.NotNull(gameListBox);

        var names = GetListBoxItemNames(gameListBox.AsListBox());
        Assert.Single(names);
        Assert.Contains("TestWebSaver", names);

        ShutdownApp();
    }

    /// <summary>
    /// ListBoxItem の Name プロパティは型名を返すため、
    /// 子要素の TextBlock からバインドされた名前を取得する。
    /// GameListPaneControl.xaml では Text="{Binding Name}" でバインドされている。
    /// </summary>
    private static System.Collections.Generic.List<string> GetListBoxItemNames(ListBox listBox)
    {
        var names = new System.Collections.Generic.List<string>();
        foreach (var item in listBox.Items)
        {
            // ListBoxItem の子要素から TextBlock を探す
            var textBlock = item.FindFirstDescendant(cf => cf.ByControlType(ControlType.Text));
            if (textBlock != null)
                names.Add(textBlock.Name);
        }
        return names;
    }

    private static FlaUI.Core.AutomationElements.AutomationElement? WaitForDescendant(
        Window window, string automationId, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow.Add(timeout);
        while (DateTime.UtcNow < deadline)
        {
            var el = window.FindFirstDescendant(cf => cf.ByAutomationId(automationId));
            if (el != null) return el;
            Thread.Sleep(200);
        }
        return null;
    }
}
