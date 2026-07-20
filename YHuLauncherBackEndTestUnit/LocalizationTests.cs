using System.Linq;
using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;
using Xunit;

namespace YHuLauncherBackEndTestUnit;

/// <summary>
/// 各ウィンドウのタイトルが lex:Loc でローカライズされており、
/// 生のリソースキー（例: "PropertyCtxHeader"）がそのまま表示されていないか検証する。
/// </summary>
public class LocalizationTests : TestAppBase
{
    /// <summary>
    /// MainWindow のタイトルが空または生キーでないことを検証。
    /// MainWindow.xaml は Title="YuLancher" の固定値なので、そのまま出るはず。
    /// </summary>
    [Fact]
    public void MainWindow_Title_Not_RawKey()
    {
        using var automation = new UIA3Automation();
        var main = GetMainWindow(automation);
        WaitForDbCreated();

        var title = main.Title ?? "";
        Assert.False(string.IsNullOrEmpty(title), "MainWindow title is empty");
        // 生キーは通常アンダースコアや CamelCase を含む。ローカライズ済みの日本語/英語文字列であること
        Assert.DoesNotContain("CtxHeader", title);
        Assert.DoesNotContain("WindowTitle", title);

        ShutdownApp();
    }
}
