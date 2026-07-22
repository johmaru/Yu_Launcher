using YuLauncher.Core.lib;
using ApplicationJsonData = YuLauncher.Core.lib.JsonControl.ApplicationJsonData;

namespace YHuLauncherBackEndTestUnit;

/// <summary>
/// テスト用の ApplicationJsonData ファクトリ。
/// 様々な FileExtension のゲームデータを生成する。
/// </summary>
public static class TestDataFactory
{
    public static ApplicationJsonData CreateExeGame(string name = "TestExeGame", string jsonPath = "./Games/test-exe.json")
        => new()
        {
            Name = name,
            FilePath = @"C:\fake\test.exe",
            JsonPath = jsonPath,
            FileExtension = "exe",
            Memo = "",
            IsWebView = false,
            IsUseLog = false,
            Url = "",
            MultipleLaunch = [],
            WikiData = new(),
            Genre = ["Application"],
        };

    public static ApplicationJsonData CreateWebGame(string name = "TestWebGame", string jsonPath = "./Games/test-webgame.json")
        => new()
        {
            Name = name,
            FilePath = "",
            JsonPath = jsonPath,
            FileExtension = "WebGame",
            Memo = "",
            IsWebView = true,
            IsUseLog = false,
            Url = "https://example.com",
            MultipleLaunch = [],
            WikiData = new(),
            Genre = ["WebGame"],
        };

    public static ApplicationJsonData CreateWebSaver(string name = "TestWebSaver", string jsonPath = "./Games/test-websaver.json")
        => new()
        {
            Name = name,
            FilePath = "",
            JsonPath = jsonPath,
            FileExtension = "WebSaver",
            Memo = "",
            IsWebView = true,
            IsUseLog = false,
            Url = "https://example.com",
            MultipleLaunch = [],
            WikiData = new(),
            Genre = ["WebSaver"],
        };
}
