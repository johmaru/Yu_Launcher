using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.UIA3;
using Xunit;
using YuLauncher.Core.lib;

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
        main = WaitForWindow(automation, w => w.FindFirstDescendant(cf => cf.ByAutomationId("NavigationToggleButton")) != null);
        main.SetForeground();

        OpenNavigationViewPane(main);
        var toggle = main.FindFirstDescendant(cf => cf.ByAutomationId("NavigationToggleButton"));
        Assert.NotNull(toggle);
        toggle.Click();
        main = WaitForWindow(automation, w =>
            w.FindFirstDescendant(cf => cf.ByAutomationId("SettingBtn")) is { IsOffscreen: false } item &&
            item.BoundingRectangle.Width > 100);
        var settingItem = main.FindFirstDescendant(cf => cf.ByAutomationId("SettingBtn"));
        Assert.NotNull(settingItem);
        settingItem.Click();

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
            TestDataFactory.CreateExeGame() with { FilePath = System.IO.Path.Combine(Environment.SystemDirectory, "cmd.exe") },
            TestDataFactory.CreateWebGame(),
            TestDataFactory.CreateWebSaver()
        );

        using var automation = new UIA3Automation();
        _ = GetMainWindow(automation);
        WaitForDbCreated();
        WaitForLogContains("MainPage Initialized", TimeSpan.FromSeconds(10));

        // MainPage.Init() が起動時に GameList.xaml (FileExtensionFilter=All) を読み込む
        // All フィルタ: data.FileExtension != "WebGame" → exe と websaver の2件
        var names = WaitForListBoxItems(automation, expectedCount: 2, TimeSpan.FromSeconds(20));
        Assert.Equal(2, names.Count);
        Assert.Contains("TestExeGame", names);
        Assert.Contains("TestWebSaver", names);
        Assert.DoesNotContain("TestWebGame", names);

        ShutdownApp();
    }

    [Theory]
    [InlineData("en", "File not found")]
    [InlineData("ja", "ファイルが見つかりません")]
    public void GameListPage_ShowsMissingFileStatusOnlyForMissingExe(string language, string expected)
    {
        TomlControl.CreateToml(SettingsTomlPath);
        TomlControl.EditToml(SettingsTomlPath, "Language", language);
        SeedDatabase(
            TestDataFactory.CreateExeGame("RemovedExe", "./Games/removed.json")
                with { FilePath = System.IO.Path.Combine(WorkDir, "removed.exe") },
            TestDataFactory.CreateExeGame("ExistingExe", "./Games/existing.json")
                with { FilePath = System.IO.Path.Combine(Environment.SystemDirectory, "cmd.exe") }
        );

        using var automation = new UIA3Automation();
        _ = GetMainWindow(automation);
        WaitForDbCreated();
        Assert.Contains("RemovedExe", WaitForListBoxItems(automation, expectedCount: 2, TimeSpan.FromSeconds(20)));

        var currentWindow = LaunchApp().GetAllTopLevelWindows(automation)
            .First(window => window.FindFirstDescendant(cf => cf.ByAutomationId("GameListBox")) != null);
        var items = currentWindow.FindFirstDescendant(cf => cf.ByAutomationId("GameListBox"))!.AsListBox().Items;
        var missing = items.Single(item => item.FindFirstDescendant(cf => cf.ByControlType(ControlType.Text))?.Name == "RemovedExe");
        var existing = items.Single(item => item.FindFirstDescendant(cf => cf.ByControlType(ControlType.Text))?.Name == "ExistingExe");
        Assert.Contains(missing.FindAllDescendants(cf => cf.ByControlType(ControlType.Text)), text => text.Name == expected);
        Assert.DoesNotContain(existing.FindAllDescendants(cf => cf.ByControlType(ControlType.Text)), text => text.Name == expected);

        missing.Select();
        var detailStatus = currentWindow.FindFirstDescendant(cf => cf.ByAutomationId("MissingFileDetailText"));
        Assert.NotNull(detailStatus);
        Assert.Equal(expected, detailStatus.Name);

        existing.Select();
        Assert.Null(LaunchApp().GetAllTopLevelWindows(automation)
            .First(window => window.FindFirstDescendant(cf => cf.ByAutomationId("GameListBox")) != null)
            .FindFirstDescendant(cf => cf.ByAutomationId("MissingFileDetailText")));

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
        _ = GetMainWindow(automation);
        WaitForDbCreated();
        WaitForLogContains("MainPage Initialized", TimeSpan.FromSeconds(10));
        var main = WaitForWindow(automation,
            window => window.FindFirstDescendant(cf => cf.ByAutomationId("WebGameListBtn")) != null);

        OpenNavigationViewPane(main);
        var webGameItem = main.FindFirstDescendant(cf => cf.ByAutomationId("WebGameListBtn"));
        Assert.NotNull(webGameItem);
        ActivateNavItem(webGameItem);

        // ナビゲーション完了 + 非同期ロード完了後に1件表示される
        var names = WaitForListBoxItems(automation, expectedCount: 1, TimeSpan.FromSeconds(20));
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
        _ = GetMainWindow(automation);
        WaitForDbCreated();
        WaitForLogContains("MainPage Initialized", TimeSpan.FromSeconds(10));
        var main = WaitForWindow(automation,
            window => window.FindFirstDescendant(cf => cf.ByAutomationId("WebSaverBtn")) != null);

        OpenNavigationViewPane(main);
        var webSaverItem = main.FindFirstDescendant(cf => cf.ByAutomationId("WebSaverBtn"));
        Assert.NotNull(webSaverItem);
        ActivateNavItem(webSaverItem);

        var names = WaitForListBoxItems(automation, expectedCount: 1, TimeSpan.FromSeconds(20));
        Assert.Single(names);
        Assert.Contains("TestWebSaver", names);

        ShutdownApp();
    }

    [Fact]
    public void GameListPage_PreservesFiltersAndSelectionAfterPropertySave()
    {
        var executable = System.IO.Path.Combine(Environment.SystemDirectory, "whoami.exe");
        Assert.True(System.IO.File.Exists(executable));
        SeedDatabase(
            TestDataFactory.CreateExeGame("Alpha Tool", "./Games/alpha.json") with { FilePath = executable, Genre = new[] { "Work" } },
            TestDataFactory.CreateExeGame("Beta Tool", "./Games/beta.json") with { FilePath = executable, Genre = new[] { "Other" } },
            TestDataFactory.CreateExeGame("Readme", "./Games/readme.json") with { FilePath = executable, Genre = new[] { "Work" } });
        var id = GameRepository.GetAll().Single(game => game.Name == "Alpha Tool").Id;
        using var automation = new UIA3Automation();
        _ = GetMainWindow(automation);
        Assert.Equal(3, WaitForListBoxItems(automation, 3, TimeSpan.FromSeconds(20)).Count);

        Window Current() => LaunchApp().GetAllTopLevelWindows(automation)
            .First(window => window.FindFirstDescendant(cf => cf.ByAutomationId("GameListBox")) != null);
        AutomationElement Find(string name) => Current().FindFirstDescendant(cf => cf.ByAutomationId(name))!;
        Find("GenreComboBox").AsComboBox().Select("Work");
        Find("SearchBox").AsTextBox().Text = "tool";
        Assert.Equal(new[] { "Alpha Tool" }, WaitForListBoxItems(automation, 1, TimeSpan.FromSeconds(20)));
        Find("GameListBox").AsListBox().Items.Single().Select();

        void Rename(string name)
        {
            Find("PropertyButton").AsButton().Invoke();
            var owner = WaitForWindow(automation, window => window.FindFirstDescendant(cf => cf.ByAutomationId("NameBox")) != null);
            var nameBox = owner.FindFirstDescendant(cf => cf.ByAutomationId("NameBox"))!;
            nameBox.AsTextBox().Text = name;
            var save = owner.FindFirstDescendant(cf => cf.ByAutomationId("SaveButton"))!;
            if (save.IsOffscreen)
            {
                var parent = save.Parent;
                while (parent != null && !parent.Patterns.Scroll.IsSupported) parent = parent.Parent;
                Assert.NotNull(parent);
                parent.Patterns.Scroll.Pattern.SetScrollPercent(-1, 100);
            }
            save.AsButton().Invoke();
            var deadline = DateTime.UtcNow.AddSeconds(20);
            while (DateTime.UtcNow < deadline && GameRepository.GetAll().Single(game => game.Id == id).Name != name)
                Thread.Sleep(100);
            Assert.Equal(name, GameRepository.GetAll().Single(game => game.Id == id).Name);
            // 保存完了後に公開されるリロード結果まで待つ。
            WaitForWindow(automation, window =>
                window.FindFirstDescendant(cf => cf.ByAutomationId("NameBox")) == null
                && (window.FindFirstDescendant(cf => cf.ByAutomationId("DetailTitleTextBlock"))?.Name == name
                    || name == "Renamed" && window.FindFirstDescendant(cf => cf.ByAutomationId("ResetFiltersButton")) != null));
        }

        Rename("Alpha Tool Updated");
        Assert.Equal("tool", Find("SearchBox").AsTextBox().Text);
        Assert.Equal("Work", Find("GenreComboBox").AsComboBox().SelectedItem!.Text);
        Assert.Equal(new[] { "Alpha Tool Updated" }, GetListBoxItemNames(Find("GameListBox").AsListBox()));
        Assert.True(Find("GameListBox").AsListBox().Items.Single().IsSelected);
        Assert.Equal("Alpha Tool Updated", Find("DetailTitleTextBlock").Name);

        Rename("Renamed");
        Assert.Empty(WaitForListBoxItems(automation, 0, TimeSpan.FromSeconds(20)));
        Assert.Null(Find("GameListBox").AsListBox().SelectedItem);
        Assert.Null(Current().FindFirstDescendant(cf => cf.ByAutomationId("PlayButton")));
        Assert.NotNull(Current().FindFirstDescendant(cf => cf.ByAutomationId("ResetFiltersButton")));
        Find("ResetFiltersButton").AsButton().Invoke();
        Assert.Equal(3, WaitForListBoxItems(automation, 3, TimeSpan.FromSeconds(20)).Count);
        Assert.Equal("", Find("SearchBox").AsTextBox().Text);
        Assert.Equal(Find("GenreComboBox").AsComboBox().Items[0].Text, Find("GenreComboBox").AsComboBox().SelectedItem!.Text);
        Assert.Null(Find("GameListBox").AsListBox().SelectedItem);
        ShutdownApp();
    }

    /// <summary>
    /// GameListBox が表示され、指定件数のアイテムがロードされるまで待つ。
    /// 非同期ロード（LoadAllGames）の完了を待つため、アイテム数が期待値に一致するまでポーリングする。
    /// </summary>
    private List<string> WaitForListBoxItems(UIA3Automation automation, int expectedCount, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow.Add(timeout);
        List<string> lastNames = new();
        while (DateTime.UtcNow < deadline)
        {
            // GetMainWindow acquired during startup can hold an outdated UIA tree after Frame navigation.
            var listBox = LaunchApp().GetAllTopLevelWindows(automation)
                .Select(window => window.FindFirstDescendant(cf => cf.ByAutomationId("GameListBox")))
                .FirstOrDefault(element => element != null)?.AsListBox();
            if (listBox != null)
            {
                var items = listBox.Items;
                lastNames = GetListBoxItemNames(listBox);
                if (items.Length == expectedCount)
                    return lastNames;
            }
            Thread.Sleep(300);
        }
        return lastNames;
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
