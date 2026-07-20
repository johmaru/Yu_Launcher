using System.Diagnostics;
using System.IO;
using FlaUI.Core;
using FlaUI.UIA3;
using System.Threading;
using Xunit;

namespace YHuLauncherBackEndTestUnit;

/// <summary>
/// FlaUI による UI 自動テスト。
/// 各テストは一時ディレクトリを CWD として exe を起動し、
/// 環境変数 YULAUNCHER_TEST_DB で %APPDATA%/YuLauncher/games.db を隔離する。
/// </summary>
public class AppLaunchTests : IDisposable
{
    private readonly string _exeDir =
        Path.GetFullPath(@"..\..\..\..\YuLauncher\bin\Debug\net9.0-windows");

    private readonly string _exePath;
    private readonly string _workDir =
        Path.Combine(Path.GetTempPath(), "YuLauncherTests_" + Guid.NewGuid().ToString("N"));

    private readonly string _testDbPath;

    public AppLaunchTests()
    {
        _exePath = Path.Combine(_exeDir, "YuLauncher.exe");
        Directory.CreateDirectory(_workDir);
        _testDbPath = Path.Combine(_workDir, "games.db");

        // Program.Main が CWD から NLog.config を読むため必須コピー
        var nlogSrc = Path.Combine(_exeDir, "NLog.config");
        Assert.True(File.Exists(nlogSrc), $"NLog.config not found in exe dir: {nlogSrc}");
        File.Copy(nlogSrc, Path.Combine(_workDir, "NLog.config"), overwrite: true);
    }

    [Fact]
    public void Launch_MainWindow_Shown_And_TestDb_Created()
    {
        Assert.True(File.Exists(_exePath), $"Exe not found: {_exePath}. Build YuLauncher (Debug) first.");

        var psi = new ProcessStartInfo
        {
            FileName = _exePath,
            WorkingDirectory = _workDir,
            UseShellExecute = false,
        };
        psi.EnvironmentVariables["YULAUNCHER_TEST_DB"] = _testDbPath;

        using var app = Application.Launch(psi);
        Assert.NotNull(app);
        try
        {
            using var automation = new UIA3Automation();
            var window = app.GetMainWindow(automation, TimeSpan.FromSeconds(30));
            Assert.NotNull(window);
            Assert.False(string.IsNullOrEmpty(window.Title));

            // 隔離DBが実際に作成されていること（環境変数オーバーライドの確認）
            // Application_Startup は Initialize(ウィンドウ表示) → InitializeDatabase(DB作成) の順なので
            // GetMainWindow が返った直後はDB未作成の可能性がある。タイムアウト付きポーリングで待つ。
            var deadline = DateTime.UtcNow.AddSeconds(15);
            while (DateTime.UtcNow < deadline && !File.Exists(_testDbPath))
                Thread.Sleep(200);
            Assert.True(File.Exists(_testDbPath),
                $"Test DB was not created at {_testDbPath}. " +
                "Exe may be stale — rebuild YuLauncher after GameRepository.DbPath edit.");
        }
        finally
        {
            if (!app.HasExited)
                app.Close();
        }
    }

    public void Dispose()
    {
        try { Directory.Delete(_workDir, recursive: true); } catch { }
    }
}
