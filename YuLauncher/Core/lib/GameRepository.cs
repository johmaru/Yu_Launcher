using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dapper;
using Microsoft.Data.Sqlite;
using YuLauncher.Core.lib;
using ApplicationJsonData = YuLauncher.Core.lib.JsonControl.ApplicationJsonData;

namespace YuLauncher.Core.lib;

public static class GameRepository
{
    public static string DbPath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                     "YuLauncher", "games.db");

    public static SqliteConnection CreateConnection()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(DbPath)!);
        var conn = new SqliteConnection($"Data Source={DbPath}");
        conn.Open();
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "PRAGMA foreign_keys = ON;";
            cmd.ExecuteNonQuery();
        }
        return conn;
    }

    // ===== パス正規化（境界でバックスラッシュ→スラッシュ） =====
    private static string NormalizePath(string jsonPath) =>
        string.IsNullOrEmpty(jsonPath) ? jsonPath : jsonPath.Replace('\\', '/');

    // ===== マイグレーション =====
    public static void RunMigrations()
    {
        using var conn = CreateConnection();
        using var tx = conn.BeginTransaction();

        using (var cmd = conn.CreateCommand())
        {
            cmd.Transaction = tx;
            cmd.CommandText = """
                CREATE TABLE IF NOT EXISTS schema_versions (
                    version INTEGER PRIMARY KEY,
                    applied_at TEXT NOT NULL DEFAULT (datetime('now'))
                );
                """;
            cmd.ExecuteNonQuery();
        }

        int currentVersion = 0;
        using (var cmd = conn.CreateCommand())
        {
            cmd.Transaction = tx;
            cmd.CommandText = "SELECT MAX(version) FROM schema_versions";
            var result = cmd.ExecuteScalar();
            if (result != DBNull.Value && result != null)
                currentVersion = Convert.ToInt32(result);
        }

        if (currentVersion < 1)
        {
            var v1Statements = new[]
            {
                """
                CREATE TABLE games (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    name TEXT NOT NULL,
                    file_path TEXT NOT NULL DEFAULT '',
                    original_json_path TEXT NOT NULL UNIQUE DEFAULT '',
                    file_extension TEXT NOT NULL DEFAULT '',
                    memo TEXT NOT NULL DEFAULT '',
                    is_web_view INTEGER NOT NULL DEFAULT 0,
                    is_use_log INTEGER NOT NULL DEFAULT 0,
                    url TEXT NOT NULL DEFAULT '',
                    is_mute INTEGER NOT NULL DEFAULT 0,
                    volume REAL,
                    created_at TEXT NOT NULL DEFAULT (datetime('now')),
                    updated_at TEXT NOT NULL DEFAULT (datetime('now'))
                );
                """,
                "CREATE INDEX idx_games_name ON games(name);",
                """
                CREATE TABLE genres (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    name TEXT NOT NULL UNIQUE
                );
                """,
                """
                CREATE TABLE game_genres (
                    game_id INTEGER NOT NULL,
                    genre_id INTEGER NOT NULL,
                    PRIMARY KEY (game_id, genre_id),
                    FOREIGN KEY (game_id) REFERENCES games(id) ON DELETE CASCADE,
                    FOREIGN KEY (genre_id) REFERENCES genres(id) ON DELETE CASCADE
                );
                """,
                """
                CREATE TABLE wiki_data (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    game_id INTEGER NOT NULL,
                    key TEXT NOT NULL,
                    value TEXT NOT NULL,
                    UNIQUE (game_id, key),
                    FOREIGN KEY (game_id) REFERENCES games(id) ON DELETE CASCADE
                );
                """,
                """
                CREATE TABLE game_multiple_launch (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    game_id INTEGER NOT NULL,
                    target_game_id INTEGER NOT NULL,
                    sort_order INTEGER NOT NULL DEFAULT 0,
                    FOREIGN KEY (game_id) REFERENCES games(id) ON DELETE CASCADE,
                    FOREIGN KEY (target_game_id) REFERENCES games(id) ON DELETE CASCADE
                );
                """,
                """
                CREATE TABLE play_history (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    game_id INTEGER NOT NULL,
                    played_at TEXT NOT NULL DEFAULT (datetime('now')),
                    FOREIGN KEY (game_id) REFERENCES games(id) ON DELETE CASCADE
                );
                """,
                "CREATE INDEX idx_play_history_game_id ON play_history(game_id);",
                "CREATE INDEX idx_play_history_played_at ON play_history(played_at);"
            };

            foreach (var sql in v1Statements)
            {
                using var c = conn.CreateCommand();
                c.Transaction = tx;
                c.CommandText = sql;
                c.ExecuteNonQuery();
            }

            using var verCmd = conn.CreateCommand();
            verCmd.Transaction = tx;
            verCmd.CommandText = "INSERT INTO schema_versions (version) VALUES (1)";
            verCmd.ExecuteNonQuery();
        }

        tx.Commit();
    }

    // ===== 存在チェック =====
    public static bool ExistsByJsonPath(string jsonPath)
    {
        using var conn = CreateConnection();
        var count = conn.ExecuteScalar<long>(
            "SELECT COUNT(*) FROM games WHERE original_json_path = @p;",
            new { p = NormalizePath(jsonPath) });
        return count > 0;
    }

    // ===== CREATE =====
    public static long InsertGame(ApplicationJsonData data)
    {
        using var conn = CreateConnection();
        using var tx = conn.BeginTransaction();

        var gameId = conn.ExecuteScalar<long>(
            """
            INSERT INTO games (name, file_path, original_json_path, file_extension, memo,
                               is_web_view, is_use_log, url, is_mute, volume)
            VALUES (@Name, @FilePath, @JsonPath, @FileExtension, @Memo,
                    @IsWebView, @IsUseLog, @Url, @IsMute, @Volume);
            SELECT last_insert_rowid();
            """,
            new
            {
                data.Name,
                FilePath = data.FilePath ?? "",
                JsonPath = NormalizePath(data.JsonPath ?? ""),
                FileExtension = data.FileExtension ?? "",
                Memo = data.Memo ?? "",
                IsWebView = (data.IsWebView ?? false) ? 1 : 0,
                IsUseLog = (data.IsUseLog ?? false) ? 1 : 0,
                Url = data.Url ?? "",
                data.IsMute,
                data.Volume
            }, tx);

        SetGenres(conn, tx, gameId, data.Genre ?? Array.Empty<string>());
        SetWikiData(conn, tx, gameId, data.WikiData ?? new());
        SetMultipleLaunch(conn, tx, gameId, data.MultipleLaunch ?? Array.Empty<string>());

        tx.Commit();
        return gameId;
    }

    // ===== READ =====
    public static ApplicationJsonData? GetById(long id)
    {
        using var conn = CreateConnection();
        return LoadSingle(conn, "WHERE g.id = @id", new { id });
    }

    public static ApplicationJsonData? GetByJsonPath(string jsonPath)
    {
        using var conn = CreateConnection();
        return LoadSingle(conn, "WHERE g.original_json_path = @p", new { p = NormalizePath(jsonPath) });
    }

    public static List<ApplicationJsonData> GetByName(string name)
    {
        using var conn = CreateConnection();
        return LoadList(conn, "WHERE g.name = @name", new { name });
    }

    public static List<ApplicationJsonData> GetAll()
    {
        using var conn = CreateConnection();
        return LoadList(conn, "", null);
    }

    public static List<ApplicationJsonData> GetByFileExtension(string ext)
    {
        using var conn = CreateConnection();
        return LoadList(conn, "WHERE g.file_extension = @ext", new { ext });
    }

    public static List<ApplicationJsonData> GetByGenre(string genreName)
    {
        using var conn = CreateConnection();
        const string where = """
            WHERE g.id IN (
                SELECT gg.game_id FROM game_genres gg
                JOIN genres gr ON gr.id = gg.genre_id
                WHERE gr.name = @genre
            )
            """;
        return LoadList(conn, where, new { genre = genreName });
    }

    public static List<ApplicationJsonData> SearchByName(string partialName)
    {
        using var conn = CreateConnection();
        return LoadList(conn, "WHERE g.name LIKE @p", new { p = $"%{partialName}%" });
    }

    public static string[] GetMultipleLaunchTargetNames(long gameId)
    {
        using var conn = CreateConnection();
        const string sql = """
            SELECT g.name FROM games g
            JOIN game_multiple_launch ml ON ml.target_game_id = g.id
            WHERE ml.game_id = @gameId
            ORDER BY ml.sort_order;
            """;
        return conn.Query<string>(sql, new { gameId }).ToArray();
    }

    public static long GetIdByJsonPath(string jsonPath)
    {
        using var conn = CreateConnection();
        var id = conn.ExecuteScalar<long?>(
            "SELECT id FROM games WHERE original_json_path = @p;",
            new { p = NormalizePath(jsonPath) });
        return id ?? -1;
    }

    // ===== UPDATE =====
    public static void UpdateGame(ApplicationJsonData data)
    {
        using var conn = CreateConnection();
        using var tx = conn.BeginTransaction();

        var affected = conn.Execute(
            """
            UPDATE games
            SET name = @Name,
                file_path = @FilePath,
                original_json_path = @JsonPath,
                file_extension = @FileExtension,
                memo = @Memo,
                is_web_view = @IsWebView,
                is_use_log = @IsUseLog,
                url = @Url,
                is_mute = @IsMute,
                volume = @Volume,
                updated_at = datetime('now')
            WHERE id = @Id;
            """,
            new
            {
                data.Id,
                data.Name,
                FilePath = data.FilePath ?? "",
                JsonPath = NormalizePath(data.JsonPath ?? ""),
                FileExtension = data.FileExtension ?? "",
                Memo = data.Memo ?? "",
                IsWebView = (data.IsWebView ?? false) ? 1 : 0,
                IsUseLog = (data.IsUseLog ?? false) ? 1 : 0,
                Url = data.Url ?? "",
                data.IsMute,
                data.Volume
            }, tx);

        if (affected == 0)
        {
            tx.Rollback();
            LoggerController.LogError($"UpdateGame: game id={data.Id} not found");
            return;
        }

        SetGenres(conn, tx, data.Id, data.Genre ?? Array.Empty<string>());
        SetWikiData(conn, tx, data.Id, data.WikiData ?? new());
        SetMultipleLaunch(conn, tx, data.Id, data.MultipleLaunch ?? Array.Empty<string>());

        tx.Commit();
    }

    public static void UpdateMemo(long gameId, string memo)
    {
        using var conn = CreateConnection();
        conn.Execute(
            "UPDATE games SET memo = @memo, updated_at = datetime('now') WHERE id = @gameId;",
            new { gameId, memo });
    }

    public static void UpdateVolume(long gameId, double volume)
    {
        using var conn = CreateConnection();
        conn.Execute(
            "UPDATE games SET volume = @volume, updated_at = datetime('now') WHERE id = @gameId;",
            new { gameId, volume });
    }

    public static void UpdateName(long gameId, string newName)
    {
        using var conn = CreateConnection();
        conn.Execute(
            "UPDATE games SET name = @newName, updated_at = datetime('now') WHERE id = @gameId;",
            new { gameId, newName });
    }

    // ===== DELETE =====
    public static void DeleteGame(long gameId)
    {
        using var conn = CreateConnection();
        conn.Execute("DELETE FROM games WHERE id = @gameId;", new { gameId });
    }

    public static void DeleteGameByJsonPath(string jsonPath)
    {
        using var conn = CreateConnection();
        conn.Execute(
            "DELETE FROM games WHERE original_json_path = @p;",
            new { p = NormalizePath(jsonPath) });
    }

    // ===== 履歴 =====
    public static void RecordPlay(long gameId)
    {
        if (gameId <= 0) return;
        try
        {
            using var conn = CreateConnection();
            conn.Execute(
                "INSERT INTO play_history (game_id) VALUES (@gameId);",
                new { gameId });
        }
        catch (Exception e)
        {
            LoggerController.LogError($"RecordPlay failed for gameId={gameId}: {e}");
        }
    }

    public static List<PlayHistoryEntry> GetPlayHistory(long gameId, int limit = 50)
    {
        using var conn = CreateConnection();
        const string sql = """
            SELECT id AS Id, game_id AS GameId, played_at AS PlayedAt
            FROM play_history
            WHERE game_id = @gameId
            ORDER BY played_at DESC
            LIMIT @limit;
            """;
        var rows = conn.Query<PlayHistoryRow>(sql, new { gameId, limit });
        return rows
            .Select(r => new PlayHistoryEntry(r.Id, r.GameId, DateTime.Parse(r.PlayedAt)))
            .ToList();
    }

    private sealed class PlayHistoryRow
    {
        public long Id { get; set; }
        public long GameId { get; set; }
        public string PlayedAt { get; set; } = "";
    }

    // ===== private ヘルパー =====
    private static void SetGenres(SqliteConnection conn, SqliteTransaction tx, long gameId, string[] genres)
    {
        conn.Execute("DELETE FROM game_genres WHERE game_id = @gameId;", new { gameId }, tx);
        foreach (var g in genres.Distinct())
        {
            if (string.IsNullOrEmpty(g)) continue;
            conn.Execute(
                """
                INSERT OR IGNORE INTO genres (name) VALUES (@g);
                INSERT OR IGNORE INTO game_genres (game_id, genre_id)
                    VALUES (@gameId, (SELECT id FROM genres WHERE name = @g));
                """,
                new { gameId, g }, tx);
        }
    }

    private static void SetWikiData(SqliteConnection conn, SqliteTransaction tx, long gameId, Dictionary<string, string> wiki)
    {
        conn.Execute("DELETE FROM wiki_data WHERE game_id = @gameId;", new { gameId }, tx);
        foreach (var kv in wiki)
        {
            if (string.IsNullOrEmpty(kv.Key)) continue;
            conn.Execute(
                "INSERT OR IGNORE INTO wiki_data (game_id, key, value) VALUES (@gameId, @k, @v);",
                new { gameId, k = kv.Key, v = kv.Value ?? "" }, tx);
        }
    }

    private static void SetMultipleLaunch(SqliteConnection conn, SqliteTransaction tx, long gameId, string[] targetNames)
    {
        conn.Execute("DELETE FROM game_multiple_launch WHERE game_id = @gameId;", new { gameId }, tx);
        int order = 0;
        foreach (var name in targetNames)
        {
            if (string.IsNullOrEmpty(name)) continue;
            var targetId = conn.ExecuteScalar<long?>(
                "SELECT id FROM games WHERE name = @name LIMIT 1;",
                new { name }, tx);
            if (targetId == null)
            {
                LoggerController.LogWarn($"MultipleLaunch target '{name}' not found, skipping");
                continue;
            }
            conn.Execute(
                """
                INSERT INTO game_multiple_launch (game_id, target_game_id, sort_order)
                VALUES (@gameId, @targetId, @sortOrder);
                """,
                new { gameId, targetId = targetId.Value, sortOrder = order++ }, tx);
        }
    }

    private static ApplicationJsonData? LoadSingle(SqliteConnection conn, string where, object? param)
    {
        var list = LoadList(conn, where, param);
        return list.Count > 0 ? list[0] : null;
    }

    private static List<ApplicationJsonData> LoadList(SqliteConnection conn, string where, object? param)
    {
        const string baseSql = """
            SELECT g.id AS Id, g.name AS Name, g.file_path AS FilePath, g.original_json_path AS JsonPath,
                   g.file_extension AS FileExtension, g.memo AS Memo,
                   g.is_web_view AS IsWebViewRaw, g.is_use_log AS IsUseLogRaw,
                   g.url AS Url, g.is_mute AS IsMute, g.volume AS Volume
            FROM games g
            """;
        var rows = conn.Query<GameRow>(baseSql + " " + where + ";", param);

        var result = new List<ApplicationJsonData>();
        foreach (var r in rows)
        {
            var genres = conn.Query<string>(
                "SELECT gr.name FROM game_genres gg JOIN genres gr ON gr.id = gg.genre_id WHERE gg.game_id = @id ORDER BY gr.name;",
                new { id = r.Id }).ToArray();

            var wiki = conn.Query<(string Key, string Value)>(
                "SELECT key AS Key, value AS Value FROM wiki_data WHERE game_id = @id;",
                new { id = r.Id })
                .ToDictionary(x => x.Key, x => x.Value);

            result.Add(new ApplicationJsonData
            {
                Id = r.Id,
                Name = r.Name ?? "",
                FilePath = r.FilePath ?? "",
                JsonPath = r.JsonPath ?? "",
                FileExtension = r.FileExtension,
                Memo = r.Memo ?? "",
                IsWebView = r.IsWebViewRaw == 1,
                IsUseLog = r.IsUseLogRaw == 1,
                Url = r.Url ?? "",
                IsMute = r.IsMute,
                Volume = r.Volume,
                Genre = genres,
                WikiData = wiki,
                MultipleLaunch = GetMultipleLaunchTargetNames(r.Id)
            });
        }
        return result;
    }

    private sealed class GameRow
    {
        public long Id { get; set; }
        public string Name { get; set; } = "";
        public string FilePath { get; set; } = "";
        public string JsonPath { get; set; } = "";
        public string? FileExtension { get; set; }
        public string Memo { get; set; } = "";
        public long IsWebViewRaw { get; set; }
        public long IsUseLogRaw { get; set; }
        public string Url { get; set; } = "";
        public bool IsMute { get; set; }
        public double? Volume { get; set; }
    }
}

public record PlayHistoryEntry(long Id, long GameId, DateTime PlayedAt);
