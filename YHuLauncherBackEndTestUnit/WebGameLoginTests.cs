using System.Globalization;
using Dapper;
using Xunit;
using YuLauncher.Core.lib;

namespace YHuLauncherBackEndTestUnit;

public sealed class WebGameLoginTests : TestAppBase
{
    private long AddWebGame()
    {
        SeedDatabase();
        return GameRepository.InsertGame(TestDataFactory.CreateWebGame());
    }

    [Fact]
    public void MigrationPreservesExistingDataAndIsRepeatable()
    {
        var id = AddWebGame();
        GameRepository.RecordPlay(id);
        using (var c = GameRepository.CreateConnection())
        {
            c.Execute("DROP TABLE IF EXISTS webgame_login_history; DROP TABLE IF EXISTS webgame_login_settings; DELETE FROM schema_versions WHERE version >= 3;");
        }
        GameRepository.RunMigrations();
        GameRepository.RunMigrations();
        using var conn = GameRepository.CreateConnection();
        Assert.Equal(4, conn.ExecuteScalar<int>("SELECT MAX(version) FROM schema_versions"));
        Assert.Equal(1, conn.ExecuteScalar<int>("SELECT COUNT(*) FROM games"));
        Assert.Equal(1, conn.ExecuteScalar<int>("SELECT COUNT(*) FROM game_genres"));
        Assert.Single(GameRepository.GetPlayHistory(id));
        Assert.Empty(WebGameLoginRepository.GetHistory(id));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MigrationFromV3PreservesHistoryAndDeletedHighWaterMark(bool deleteAll)
    {
        var id = AddWebGame();
        using (var c = GameRepository.CreateConnection())
        {
            c.Execute("""
                DROP TABLE webgame_login_history;
                DROP TABLE webgame_login_settings;
                DELETE FROM schema_versions WHERE version >= 3;
                CREATE TABLE webgame_login_settings (
                    game_id INTEGER PRIMARY KEY, mode TEXT NOT NULL CHECK(mode IN ('Connection','Url','Element','JavaScript')),
                    target_origin TEXT NOT NULL, success_url TEXT NOT NULL, success_selector TEXT NOT NULL, javascript TEXT NOT NULL,
                    FOREIGN KEY(game_id) REFERENCES games(id) ON DELETE CASCADE);
                CREATE TABLE webgame_login_history (
                    id INTEGER PRIMARY KEY AUTOINCREMENT, game_id INTEGER NOT NULL, session_id TEXT NOT NULL,
                    detected_at_utc TEXT NOT NULL, method TEXT NOT NULL CHECK(method IN ('Connection','Url','Element','JavaScript')),
                    UNIQUE(game_id,session_id), FOREIGN KEY(game_id) REFERENCES games(id) ON DELETE CASCADE);
                CREATE INDEX idx_webgame_login_history_game_time ON webgame_login_history(game_id,detected_at_utc DESC,id DESC);
                INSERT INTO schema_versions(version) VALUES(3);
                """);
            foreach (var mode in new[] { "Connection", "Url", "Element", "JavaScript" })
            {
                var other = GameRepository.InsertGame(TestDataFactory.CreateWebGame(mode, $"./Games/{mode}.json"));
                c.Execute("INSERT INTO webgame_login_settings VALUES(@other,@mode,'https://example.com','https://example.com/home','#user','yuLogin.complete();')", new { other, mode });
            }
            c.Execute("INSERT INTO webgame_login_history VALUES(10,@id,'retained','2026-09-30T00:00:00.0000000+00:00','Url'); INSERT INTO webgame_login_history VALUES(100,@id,'deleted','2026-09-30T00:00:00.0000000+00:00','JavaScript'); DELETE FROM webgame_login_history WHERE id=100", new { id });
            if (deleteAll) c.Execute("DELETE FROM webgame_login_history");
        }
        GameRepository.RunMigrations();
        GameRepository.RunMigrations();
        using var conn = GameRepository.CreateConnection();
        Assert.Equal(4, conn.ExecuteScalar<int>("SELECT MAX(version) FROM schema_versions"));
        Assert.Equal(4, conn.ExecuteScalar<int>("SELECT COUNT(*) FROM webgame_login_settings WHERE network_rule_json=''"));
        if (deleteAll) Assert.Empty(WebGameLoginRepository.GetHistory(id));
        else Assert.Equal(10, Assert.Single(WebGameLoginRepository.GetHistory(id)).Id);
        Assert.True(WebGameLoginRepository.RecordLogin(id, "new", WebGameLoginMode.Url, DateTimeOffset.UtcNow));
        Assert.True(WebGameLoginRepository.GetHistory(id)[0].Id > 100);
    }

    [Fact]
    public void RecordsOnlyWebGamesAndDeduplicatesSessions()
    {
        var id = AddWebGame();
        var time = DateTimeOffset.Parse("2026-09-30T10:00:00+09:00");
        Assert.True(WebGameLoginRepository.RecordLogin(id, "shared", WebGameLoginMode.JavaScript, time));
        Assert.False(WebGameLoginRepository.RecordLogin(id, "shared", WebGameLoginMode.JavaScript, time));
        Assert.True(WebGameLoginRepository.RecordLogin(id, "independent", WebGameLoginMode.Url, time));
        foreach (var type in new[] { "exe", "web", "WebSaver" })
        {
            var other = GameRepository.InsertGame(TestDataFactory.CreateExeGame(type, $"./Games/{type}.json") with { FileExtension = type });
            Assert.False(WebGameLoginRepository.RecordLogin(other, "session", WebGameLoginMode.Connection, time));
            Assert.Throws<InvalidOperationException>(() => WebGameLoginRepository.GetSettings(other));
        }
        Assert.False(WebGameLoginRepository.RecordLogin(0, "s", WebGameLoginMode.Connection, time));
        Assert.False(WebGameLoginRepository.RecordLogin(id, " ", WebGameLoginMode.Connection, time));
        Assert.False(WebGameLoginRepository.RecordLogin(long.MaxValue, "s", WebGameLoginMode.Connection, time));
        Assert.Equal(2, WebGameLoginRepository.GetHistory(id).Count);
        Assert.All(WebGameLoginRepository.GetHistory(id), e => Assert.Equal(time, e.DetectedAtUtc));
    }

    [Fact]
    public void SubscriberFailureDoesNotUndoSavedRecordOrSkipOtherSubscribers()
    {
        var id = AddWebGame();
        Action<long> failed = _ => throw new InvalidOperationException("fixture");
        long? notified = null;
        Action<long> observer = gameId => notified = gameId;
        WebGameLoginRepository.LoginRecorded += failed;
        WebGameLoginRepository.LoginRecorded += observer;
        try
        {
            Assert.True(WebGameLoginRepository.RecordLogin(id, "s", WebGameLoginMode.JavaScript, DateTimeOffset.UtcNow));
            Assert.Equal(id, notified);
            Assert.Single(WebGameLoginRepository.GetHistory(id));
        }
        finally
        {
            WebGameLoginRepository.LoginRecorded -= failed;
            WebGameLoginRepository.LoginRecorded -= observer;
        }
    }

    [Fact]
    public void SettingsSurviveGameUpdatesAndDeleteWithGame()
    {
        var id = AddWebGame();
        foreach (var mode in Enum.GetValues<WebGameLoginMode>())
        {
            var setting = new WebGameLoginSettings(mode, "https://example.com", "https://example.com/home", "#user-menu", "yuLogin.complete();",
                mode == WebGameLoginMode.Network ? new("https://api.example.test/login", "POST", 200, "/data/success", "true") : null);
            WebGameLoginRepository.SaveSettings(id, setting);
            Assert.Equal(setting, WebGameLoginRepository.GetSettings(id));
            GameRepository.UpdateVolume(id, .5);
            Assert.Equal(setting, WebGameLoginRepository.GetSettings(id));
        }
        var beforeRename = WebGameLoginRepository.GetSettings(id);
        Assert.True(WebGameLoginRepository.RecordLogin(id, "before-rename", WebGameLoginMode.JavaScript, DateTimeOffset.UtcNow));
        var game = TestDataFactory.CreateWebGame() with { Id = id, Name = "Renamed", Url = "https://new.example.com/game" };
        GameRepository.UpdateGame(game);
        Assert.Equal(beforeRename, WebGameLoginRepository.GetSettings(id));
        Assert.Single(WebGameLoginRepository.GetHistory(id));
        WebGameLoginRepository.SaveSettings(id, new(WebGameLoginMode.Connection, "https://example.com", "", "", ""));
        Assert.Equal("https://new.example.com", WebGameLoginRepository.GetSettings(id).TargetOrigin);
        Assert.True(WebGameLoginRepository.RecordLogin(id, "s", WebGameLoginMode.Connection, DateTimeOffset.UtcNow));
        GameRepository.DeleteGameByJsonPath(game.JsonPath);
        using var c = GameRepository.CreateConnection();
        Assert.Equal(0, c.ExecuteScalar<int>("SELECT COUNT(*) FROM webgame_login_settings"));
        Assert.Equal(0, c.ExecuteScalar<int>("SELECT COUNT(*) FROM webgame_login_history"));
    }

    [Fact]
    public void InvalidSettingsDoNotReplaceSavedSettings()
    {
        var id = AddWebGame();
        var saved = WebGameLoginRepository.GetSettings(id);
        foreach (var bad in new[]
        {
            saved with { TargetOrigin = "file:///tmp" },
            saved with { TargetOrigin = "https://example.com/path" },
            saved with { Mode = WebGameLoginMode.Url, SuccessUrl = "https://other.example.com/home" },
            saved with { Mode = WebGameLoginMode.Element, SuccessSelector = " " },
            saved with { Mode = WebGameLoginMode.JavaScript, JavaScript = " " },
            saved with { Mode = (WebGameLoginMode)99 }
        }) Assert.Throws<ArgumentException>(() => WebGameLoginRepository.SaveSettings(id, bad));
        Assert.Equal(saved, WebGameLoginRepository.GetSettings(id));
    }

    [Fact]
    public void InvalidOrCorruptNetworkRulesNeverBecomeDefaultSettings()
    {
        var id = AddWebGame();
        var saved = new WebGameLoginSettings(WebGameLoginMode.Network, "https://example.com", "", "", "",
            new("https://api.example.test/login", "POST", 200, "/success", "true"));
        WebGameLoginRepository.SaveSettings(id, saved);
        Assert.Throws<ArgumentException>(() => WebGameLoginRepository.SaveSettings(id, saved with { NetworkRule = null }));
        Assert.Throws<ArgumentException>(() => WebGameLoginRepository.SaveSettings(id, saved with
        { Mode = WebGameLoginMode.Connection, NetworkRule = saved.NetworkRule! with { Method = "bad method" } }));
        Assert.Equal(saved, WebGameLoginRepository.GetSettings(id));
        using var c = GameRepository.CreateConnection();
        c.Execute("UPDATE webgame_login_settings SET network_rule_json='broken' WHERE game_id=@id", new { id });
        Assert.Throws<System.Text.Json.JsonException>(() => WebGameLoginRepository.GetSettings(id));
        Assert.Equal("broken", c.ExecuteScalar<string>("SELECT network_rule_json FROM webgame_login_settings WHERE game_id=@id", new { id }));
        Assert.True(WebGameLoginRepository.RecordLogin(id, "network", WebGameLoginMode.Network, DateTimeOffset.UtcNow));
        Assert.False(WebGameLoginRepository.RecordLogin(id, "network", WebGameLoginMode.Network, DateTimeOffset.UtcNow));
        Assert.Equal(WebGameLoginMode.Network, Assert.Single(WebGameLoginRepository.GetHistory(id)).Method);
    }

    [Fact]
    public void SummariesRespectHalfOpenDayAndPaginationIsStable()
    {
        var id = AddWebGame();
        var start = DateTimeOffset.Parse("2026-09-29T15:00:00Z", CultureInfo.InvariantCulture);
        var end = start.AddDays(1);
        WebGameLoginRepository.RecordLogin(id, "before", WebGameLoginMode.Connection, start.AddTicks(-1));
        WebGameLoginRepository.RecordLogin(id, "start", WebGameLoginMode.Url, start);
        WebGameLoginRepository.RecordLogin(id, "end", WebGameLoginMode.Element, end);
        var summary = WebGameLoginRepository.GetSummaries(new[] { id }, start, end)[id];
        Assert.True(summary.HasRecordToday);
        Assert.Equal(WebGameLoginMode.Url, summary.TodayMethod);
        Assert.Equal(end, summary.LastDetectedAtUtc);
        Assert.False(WebGameLoginRepository.GetSummaries(new[] { id }, end.AddDays(1), end.AddDays(2))[id].HasRecordToday);
        for (var i = 0; i < 101; i++)
            Assert.True(WebGameLoginRepository.RecordLogin(id, $"many{i}", WebGameLoginMode.JavaScript, end.AddHours(1)));
        var first = WebGameLoginRepository.GetHistory(id);
        var second = WebGameLoginRepository.GetHistory(id, 50);
        var third = WebGameLoginRepository.GetHistory(id, 100);
        var all = first.Concat(second).Concat(third).ToArray();
        Assert.Equal(104, all.Length);
        Assert.Equal(104, all.Select(e => e.Id).Distinct().Count());
        Assert.True(first[0].Id > first[1].Id);
        Assert.Empty(WebGameLoginRepository.GetSummaries(Array.Empty<long>(), start, end));
        Assert.Throws<ArgumentOutOfRangeException>(() => WebGameLoginRepository.GetHistory(id, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => WebGameLoginRepository.GetHistory(id, 0, 0));
    }
}
