using System;
using System.Linq;
using System.Threading;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.UIA3;
using YuLauncher.Core.lib;
using Xunit;

namespace YHuLauncherBackEndTestUnit;

/// <summary>
/// ゲーム追加→プロパティ編集→削除のCRUDフローを検証する。
/// UI操作 → DB反映を実際に確認する。
/// </summary>
public class CrudTests : TestAppBase
{
    [Fact]
    public void Database_Has_Correct_Schema()
    {
        // アプリ起動でDBマイグレーションが走る
        using var automation = new UIA3Automation();
        GetMainWindow(automation);
        WaitForDbCreated();
        ShutdownApp();

        // テストプロセスから直接DBを検証
        var allGames = GameRepository.GetAll();
        Assert.NotNull(allGames);

        // Insert → GetById → Delete のサイクル
        var gameId = GameRepository.InsertGame(TestDataFactory.CreateExeGame());
        var retrieved = GameRepository.GetById(gameId);
        Assert.NotNull(retrieved);
        Assert.Equal("TestExeGame", retrieved.Value.Name);

        GameRepository.DeleteGame(gameId);
        Assert.Null(GameRepository.GetById(gameId));
    }

    [Fact]
    public void Seed_Insert_GetAll_Delete_Full_Cycle()
    {
        SeedDatabase(
            TestDataFactory.CreateExeGame(),
            TestDataFactory.CreateWebGame(),
            TestDataFactory.CreateWebSaver()
        );

        // テストプロセスから検証
        var all = GameRepository.GetAll();
        Assert.Equal(3, all.Count);

        var exe = GameRepository.GetByName("TestExeGame");
        Assert.Single(exe);
        Assert.Equal("exe", exe[0].FileExtension);

        var web = GameRepository.GetByName("TestWebGame");
        Assert.Single(web);
        Assert.Equal("WebGame", web[0].FileExtension);

        var saver = GameRepository.GetByName("TestWebSaver");
        Assert.Single(saver);
        Assert.Equal("WebSaver", saver[0].FileExtension);

        // 削除
        GameRepository.DeleteGame(exe[0].Id);
        Assert.Equal(2, GameRepository.GetAll().Count);

        // 更新
        var webGame = web[0];
        var updated = webGame with { Name = "RenamedWebGame" };
        GameRepository.UpdateGame(updated);
        var renamed = GameRepository.GetById(webGame.Id);
        Assert.Equal("RenamedWebGame", renamed?.Name);
    }

    [Fact]
    public void GameListPage_Displays_Seeded_Games()
    {
        SeedDatabase(TestDataFactory.CreateExeGame());

        using var automation = new UIA3Automation();
        var main = GetMainWindow(automation);
        WaitForDbCreated();
        WaitForLogContains("MainPage Initialized", TimeSpan.FromSeconds(10));

        var gameListBox = WaitForDescendant(main, "GameListBox", TimeSpan.FromSeconds(15));
        Assert.NotNull(gameListBox);

        var names = GetListBoxItemNames(gameListBox.AsListBox());
        Assert.Single(names);
        Assert.Contains("TestExeGame", names);

        ShutdownApp();
    }

    private static System.Collections.Generic.List<string> GetListBoxItemNames(ListBox listBox)
    {
        var names = new System.Collections.Generic.List<string>();
        foreach (var item in listBox.Items)
        {
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
