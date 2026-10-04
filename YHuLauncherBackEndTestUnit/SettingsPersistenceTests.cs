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

    [Theory]
    [InlineData("en", "Dark")]
    [InlineData("ja", "Light")]
    public void Settings_Change_Persists_After_Restart(string language, string theme)
    {
        TomlControl.CreateToml(SettingsTomlPath);
        TomlControl.EditToml(SettingsTomlPath, "Language", language);
        TomlControl.EditToml(SettingsTomlPath, "Theme", theme);
        TomlControl.EditToml(SettingsTomlPath, "SettingResolution", "Width", "600");
        TomlControl.EditToml(SettingsTomlPath, "SettingResolution", "Height", "400");
        using var automation = new UIA3Automation();
        GetMainWindow(automation);
        WaitForDbCreated();
        Assert.True(WaitForLogContains("MainPage Initialized", TimeSpan.FromSeconds(10)));
        var settings = OpenSettingsWindow(automation);
        Assert.Equal(600, settings.BoundingRectangle.Width);
        Assert.Equal(400, settings.BoundingRectangle.Height);
        Find(settings, "DividerColorBox").AsTextBox().Text = "#336699";
        Find(settings, "DividerColorApplyBtn").AsButton().Invoke();
        Find(settings, "VideoVisualItem").Click();
        settings = WaitForSettingsWindow(automation, "MemoTxtFontSize");
        foreach (var id in new[] { "GeneralItem", "VideoVisualItem", "SaveBtn" })
            AssertVisibleInside(settings, Find(settings, id));
        var scroll = Find(settings, "MainViewer").Patterns.Scroll.Pattern;
        scroll.SetScrollPercent(-1, 0);
        AssertVisibleInside(settings, Find(settings, "SaveBtn"));
        scroll.SetScrollPercent(-1, 100);
        AssertVisibleInside(settings, Find(settings, "SaveBtn"));
        foreach (var (id, _, _, value) in SizeInputs)
            Find(settings, id).AsTextBox().Text = value;
        Find(settings, "MemoTxtFontSize").AsTextBox().Text = "24";
        var targetLanguage = language == "en" ? "ja" : "en";
        scroll.SetScrollPercent(-1, 0);
        Find(settings, "LanguageCombo").AsComboBox().Select(targetLanguage == "en" ? 0 : 1);
        Find(settings, "FullSc").AsCheckBox().IsChecked = true;
        Find(settings, "VideoVisualItem").Click();
        settings = WaitForSettingsWindow(automation, "MemoTxtFontSize");
        Assert.Equal("24", Find(settings, "MemoTxtFontSize").AsTextBox().Text);
        Find(settings, "SaveBtn").AsButton().Invoke();
        var deadline = DateTime.UtcNow.AddSeconds(20);
        while (TomlControl.GetTomlString(SettingsTomlPath, "MemoFontSize") != "24" && DateTime.UtcNow < deadline)
            Thread.Sleep(200);
        AssertSaved(targetLanguage, theme);
        ShutdownApp();

        GetMainWindow(automation);
        WaitForDbCreated();
        Assert.True(WaitForLogContains("MainPage Initialized", TimeSpan.FromSeconds(10)));
        AssertSaved(targetLanguage, theme);
        settings = OpenSettingsWindow(automation);
        Assert.Equal("#336699", Find(settings, "DividerColorBox").AsTextBox().Text);
        Find(settings, "VideoVisualItem").Click();
        settings = WaitForSettingsWindow(automation, "MemoTxtFontSize");
        foreach (var (id, _, _, value) in SizeInputs)
            Assert.Equal(value, Find(settings, id).AsTextBox().Text);
        Assert.Equal("24", Find(settings, "MemoTxtFontSize").AsTextBox().Text);
        Assert.Equal(targetLanguage == "en" ? "EngItemCombo" : "JpnItemCombo",
            Find(settings, "LanguageCombo").AsComboBox().SelectedItem?.AutomationId);
        Assert.True(Find(settings, "FullSc").AsCheckBox().IsChecked);
        ShutdownApp();
    }

    private static readonly (string Id, string Section, string Key, string Value)[] SizeInputs =
    [
        ("TxtResWidth", "SettingResolution", "Width", "920"),
        ("TxtResHeight", "SettingResolution", "Height", "600"),
        ("MenuTxtResWidth", "WindowResolution", "Width", "1024"),
        ("MenuTxtResHeight", "WindowResolution", "Height", "720"),
        ("GameTxtResWidth", "GameResolution", "Width", "1280"),
        ("GameTxtResHeight", "GameResolution", "Height", "720"),
        ("MemoTxtResWidth", "MemoResolution", "Width", "680"),
        ("MemoTxtResHeight", "MemoResolution", "Height", "300")
    ];

    private void AssertSaved(string language, string theme)
    {
        foreach (var (_, section, key, value) in SizeInputs)
            Assert.Equal(value, TomlControl.GetTomlString(SettingsTomlPath, section, key));
        Assert.Equal("24", TomlControl.GetTomlString(SettingsTomlPath, "MemoFontSize"));
        Assert.Equal(language, TomlControl.GetTomlString(SettingsTomlPath, "Language"));
        Assert.Equal(theme, TomlControl.GetTomlString(SettingsTomlPath, "Theme"));
        Assert.Equal("true", TomlControl.GetTomlString(SettingsTomlPath, "FullScreen"));
        Assert.Equal("#336699", TomlControl.GetTomlString(SettingsTomlPath, "DividerColor"));
    }

    private Window OpenSettingsWindow(UIA3Automation automation)
    {
        var main = WaitForWindow(automation, w => w.FindFirstDescendant(cf => cf.ByAutomationId("NavigationToggleButton")) != null);
        main.SetForeground();
        OpenNavigationViewPane(main);
        Find(main, "NavigationToggleButton").Click();
        main = WaitForWindow(automation, w =>
            w.FindFirstDescendant(cf => cf.ByAutomationId("SettingBtn")) is { IsOffscreen: false } item &&
            item.BoundingRectangle.Width > 100);
        Find(main, "SettingBtn").Click();
        return WaitForSettingsWindow(automation, "DividerColorBox");
    }

    private Window WaitForSettingsWindow(UIA3Automation automation, string id)
    {
        Window? FindSettings(Window owner) => owner
            .FindAllDescendants(cf => cf.ByControlType(FlaUI.Core.Definitions.ControlType.Window))
            .FirstOrDefault(w => w.IsEnabled && w.FindFirstDescendant(cf => cf.ByAutomationId(id)) != null)?.AsWindow();
        var root = WaitForWindow(automation, w => FindSettings(w) != null);
        return FindSettings(root)!;
    }

    private static AutomationElement Find(Window window, string id) =>
        window.FindFirstDescendant(cf => cf.ByAutomationId(id))
        ?? throw new InvalidOperationException($"Missing control: {id}");

    private static void AssertVisibleInside(Window window, AutomationElement element)
    {
        Assert.False(element.IsOffscreen);
        var bounds = element.BoundingRectangle;
        Assert.True(bounds.Width > 0 && bounds.Height > 0);
        Assert.True(window.BoundingRectangle.Contains(bounds), $"{element.AutomationId}: {bounds} outside {window.BoundingRectangle}");
    }
}
