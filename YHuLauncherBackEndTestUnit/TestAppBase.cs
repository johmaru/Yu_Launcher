using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;

namespace YHuLauncherBackEndTestUnit;

/// <summary>
/// FlaUI テスト共通ヘルパー。
/// 各テストクラスはこのクラスを継承してアプリ起動・終了・ウィンドウ検索を利用する。
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

        // NLogログの読み取り開始位置を記憶（起動前のログを除外するため）
        if (File.Exists(NLogPath))
            _logReadOffset = new FileInfo(NLogPath).Length;
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
    }

    /// <summary>
    /// 起動後に追加された NLog ログを読み取る。
    /// ページ遷移やウィンドウ初期化の確認に使用。
    /// </summary>
    protected string ReadNewLogLines()
    {
        if (!File.Exists(NLogPath))
            return string.Empty;

        using var fs = new FileStream(NLogPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        fs.Position = _logReadOffset;
        using var reader = new StreamReader(fs);
        return reader.ReadToEnd();
    }

    /// <summary>
    /// 指定文字列が新規ログに含まれるまで待つ。
    /// </summary>
    protected bool WaitForLogContains(string substring, TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow.Add(timeout ?? TimeSpan.FromSeconds(10));
        while (DateTime.UtcNow < deadline)
        {
            var log = ReadNewLogLines();
            if (log.Contains(substring))
                return true;
            Thread.Sleep(300);
        }
        return false;
    }

    public virtual void Dispose()
    {
        ShutdownApp();
        try { Directory.Delete(WorkDir, recursive: true); } catch { }
    }
}
