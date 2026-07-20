using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;
using YuLauncher.Core.lib;
using ApplicationJsonData = YuLauncher.Core.lib.JsonControl.ApplicationJsonData;

namespace YHuLauncherBackEndTestUnit;

/// <summary>
/// FlaUI テスト共通ヘルパー。
/// アプリ起動・終了・ウィンドウ検索・DB操作・NLog検査を提供。
/// </summary>
public abstract class TestAppBase : IDisposable
{
    private readonly string _exeDir =
        Path.GetFullPath(@"..\..\..\..\YuLauncher\bin\Debug\net9.0-windows");

    protected string ExePath { get; }
    protected string WorkDir { get; }
    protected string TestDbPath { get; }
    protected string SettingsTomlPath => Path.Combine(WorkDir, "settings.toml");
    protected string NLogPath => Path.Combine(_exeDir, "logs", "app.log");

    private Application? _app;
    private long _logReadOffset;

    protected TestAppBase()
    {
        ExePath = Path.Combine(_exeDir, "YuLauncher.exe");
        WorkDir = Path.Combine(Path.GetTempPath(), "YuLauncherTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(WorkDir);
        TestDbPath = Path.Combine(WorkDir, "games.db");

        var nlogSrc = Path.Combine(_exeDir, "NLog.config");
        if (!File.Exists(nlogSrc))
            throw new FileNotFoundException($"NLog.config not found in exe dir: {nlogSrc}");
        File.Copy(nlogSrc, Path.Combine(WorkDir, "NLog.config"), overwrite: true);

        if (File.Exists(NLogPath))
            _logReadOffset = new FileInfo(NLogPath).Length;

        // テストプロセス側でも GameRepository が同一の隔離DBを参照するように環境変数を設定。
        // アプリプロセスには ProcessStartInfo 経由でも同じ値を渡す。
        Environment.SetEnvironmentVariable("YULAUNCHER_TEST_DB", TestDbPath);
    }

    /// <summary>
    /// 隔離DBにマイグレーションを実行し、指定ゲームを挿入する。
    /// 環境変数はコンストラクタで設定済み。アプリ起動前に呼ぶこと。
    /// </summary>
    protected void SeedDatabase(params ApplicationJsonData[] games)
    {
        GameRepository.RunMigrations();
        foreach (var g in games)
            GameRepository.InsertGame(g);
    }

    protected Application LaunchApp()
    {
        if (_app != null)
            return _app;

        if (!File.Exists(ExePath))
            throw new FileNotFoundException($"Exe not found: {ExePath}. Build YuLauncher (Debug) first.");

        var psi = new ProcessStartInfo
        {
            FileName = ExePath,
            WorkingDirectory = WorkDir,
            UseShellExecute = false,
        };
        psi.EnvironmentVariables["YULAUNCHER_TEST_DB"] = TestDbPath;

        _app = Application.Launch(psi);
        return _app;
    }

    protected Window WaitForWindow(UIA3Automation automation, Func<Window, bool> predicate, TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow.Add(timeout ?? TimeSpan.FromSeconds(20));
        while (DateTime.UtcNow < deadline)
        {
            var all = _app!.GetAllTopLevelWindows(automation);
            var match = all.FirstOrDefault(predicate);
            if (match != null)
                return match;
            Thread.Sleep(200);
        }
        throw new TimeoutException($"Window matching predicate not found within {timeout}");
    }

    protected Window GetMainWindow(UIA3Automation automation)
    {
        var app = LaunchApp();
        return app.GetMainWindow(automation, TimeSpan.FromSeconds(30))
               ?? throw new InvalidOperationException("Failed to get MainWindow");
    }

    protected void WaitForDbCreated(TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow.Add(timeout ?? TimeSpan.FromSeconds(15));
        while (DateTime.UtcNow < deadline && !File.Exists(TestDbPath))
            Thread.Sleep(200);
        if (!File.Exists(TestDbPath))
            throw new TimeoutException($"Test DB was not created at {TestDbPath}");
    }

    protected void ShutdownApp()
    {
        if (_app == null) return;
        try
        {
            if (!_app.HasExited)
            {
                _app.Close();
                _app.Dispose();
            }
        }
        catch { }
        _app = null;
        Environment.SetEnvironmentVariable("YULAUNCHER_TEST_DB", null);
    }

    protected string ReadNewLogLines()
    {
        if (!File.Exists(NLogPath))
            return string.Empty;

        using var fs = new FileStream(NLogPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        fs.Position = _logReadOffset;
        using var reader = new StreamReader(fs);
        return reader.ReadToEnd();
    }

    protected bool WaitForLogContains(string substring, TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow.Add(timeout ?? TimeSpan.FromSeconds(10));
        while (DateTime.UtcNow < deadline)
        {
            if (ReadNewLogLines().Contains(substring))
                return true;
            Thread.Sleep(300);
        }
        return false;
    }

    /// <summary>
    /// NavigationView のペインを開く。
    /// MainPage.xaml で PaneDisplayMode="LeftMinimal" IsPaneOpen="False" のため必須。
    /// </summary>
    protected static void OpenNavigationViewPane(Window main)
    {
        var toggle = main.FindFirstDescendant(cf => cf.ByAutomationId("PaneToggleButton"))
                  ?? main.FindFirstDescendant(cf => cf.ByName("TogglePane"));
        if (toggle != null)
        {
            try { toggle.AsListBoxItem().Select(); } catch { }
            toggle.Focus();
            Thread.Sleep(300);
            FlaUI.Core.Input.Keyboard.TypeVirtualKeyCode((ushort)FlaUI.Core.WindowsAPI.VirtualKeyShort.RETURN);
            Thread.Sleep(800);
        }
    }

    /// <summary>
    /// NavigationViewItem をアクティブ化する。
    /// Click がルーティングされないため Select + Enter を使う。
    /// </summary>
    protected static void ActivateNavItem(FlaUI.Core.AutomationElements.AutomationElement item)
    {
        try { item.AsListBoxItem().Select(); } catch { }
        item.Focus();
        Thread.Sleep(300);
        FlaUI.Core.Input.Keyboard.TypeVirtualKeyCode((ushort)FlaUI.Core.WindowsAPI.VirtualKeyShort.RETURN);
        Thread.Sleep(1000);
    }

    public virtual void Dispose()
    {
        ShutdownApp();
        try { Directory.Delete(WorkDir, recursive: true); } catch { }
    }
}
