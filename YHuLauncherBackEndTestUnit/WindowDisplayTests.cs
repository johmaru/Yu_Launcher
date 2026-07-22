using System;
using System.Collections.Generic;
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
        var names = WaitForListBoxItems(main, expectedCount: 2, TimeSpan.FromSeconds(20));
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

        // ナビゲーション完了 + 非同期ロード完了後に1件表示される
        var names = WaitForListBoxItems(main, expectedCount: 1, TimeSpan.FromSeconds(20));
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

        var names = WaitForListBoxItems(main, expectedCount: 1, TimeSpan.FromSeconds(20));
        Assert.Single(names);
        Assert.Contains("TestWebSaver", names);

        ShutdownApp();
    }

    /// <summary>
    /// GameListBox が表示され、指定件数のアイテムがロードされるまで待つ。
    /// 非同期ロード（LoadAllGames）の完了を待つため、アイテム数が期待値に一致するまでポーリングする。
    /// </summary>
    private static List<string> WaitForListBoxItems(Window main, int expectedCount, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow.Add(timeout);
        while (DateTime.UtcNow < deadline)
        {
            var listBox = main.FindFirstDescendant(cf => cf.ByAutomationId("GameListBox"));
            if (listBox != null)
            {
                var items = listBox.AsListBox().Items;
                if (items.Length == expectedCount)
                    return GetListBoxItemNames(listBox.AsListBox());
            }
            Thread.Sleep(300);
        }
        // タイムアウト時: 最後に取得できた内容を返す（アサートで失敗させるため）
        var finalListBox = main.FindFirstDescendant(cf => cf.ByAutomationId("GameListBox"));
        return finalListBox != null
            ? GetListBoxItemNames(finalListBox.AsListBox())
            : new List<string>();
    }

    /// <summary>
    /// ListBoxItem の Name プロパティは型名を返すため、
    /// 子要素の TextBlock からバインドされた名前を取得する。
    /// GameListPaneControl.xaml では Text="{Binding Name}" でバインドされている。
    /// </summary>
    private static List<string> GetListBoxItemNames(ListBox listBox)
    {
        var names = new List<string>();
        foreach (var item in listBox.Items)
        {
            var textBlock = item.FindFirstDescendant(cf => cf.ByControlType(ControlType.Text));
            if (textBlock != null)
                names.Add(textBlock.Name);
        }
        return names;
    }
}
