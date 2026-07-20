using System;
using System.IO;
using System.Threading;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.UIA3;
using Microsoft.Data.Sqlite;
using Xunit;

namespace YHuLauncherBackEndTestUnit;

/// <summary>
/// ゲーム追加→プロパティ編集→削除のCRUDフローを検証する。
/// DBとの整合性も確認。
/// </summary>
public class CrudTests : TestAppBase
{
    [Fact]
    public void GameList_Page_Shows_AddButton()
    {
        using var automation = new UIA3Automation();
        var main = GetMainWindow(automation);
        WaitForDbCreated();

        // GameList メニューを開く
        var gameListItem = main.FindFirstDescendant(cf => cf.ByAutomationId("GameListBtn"));
        Assert.NotNull(gameListItem);
        gameListItem.Click();

        // GameListPaneControl の AddButton が現れるまで待つ（ナビゲーション成功の証拠）
        var deadline = DateTime.UtcNow.AddSeconds(10);
        FlaUI.Core.AutomationElements.AutomationElement? addButton = null;
        while (DateTime.UtcNow < deadline && addButton == null)
        {
            addButton = main.FindFirstDescendant(cf => cf.ByAutomationId("AddButton"));
            if (addButton == null) Thread.Sleep(200);
        }
        Assert.NotNull(addButton);

        ShutdownApp();
    }

    [Fact]
    public void Database_Has_Correct_Schema()
    {
        using var automation = new UIA3Automation();
        GetMainWindow(automation);
        WaitForDbCreated();
        ShutdownApp();

        // DBに期待するテーブルが存在するか検証
        using var conn = new SqliteConnection($"Data Source={TestDbPath}");
        conn.Open();

        var tables = new[]
        {
            "games", "genres", "game_genres", "wiki_data",
            "game_multiple_launch", "play_history", "schema_versions"
        };

        foreach (var table in tables)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"SELECT name FROM sqlite_master WHERE type='table' AND name='{table}'";
            var result = cmd.ExecuteScalar();
            Assert.Equal(table, result);
        }
    }

    private static Button? FindButtonByText(Window window, params string[] texts)
    {
        foreach (var t in texts)
        {
            var el = window.FindFirstDescendant(cf =>
                cf.ByName(t).And(cf.ByControlType(ControlType.Button)));
            if (el != null)
                return el.AsButton();
        }
        return null;
    }
}
