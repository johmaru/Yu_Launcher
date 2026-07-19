# ゲームデータ SQLite 移行 実装計画

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** ゲームデータ管理を `Games/*.json`（1エントリ=1ファイル）から `%APPDATA%/YuLauncher/games.db`（SQLite）に移行し、N+1読み込み・参照整合性・部分更新の課題を解決する。

**Architecture:** `GameRepository` クラスを新設し Dapper ベースでCRUDを提供。`JsonControl.ApplicationJsonData` を `record struct` 化して `Id` フィールドを追加。`JsonControl` は既存シグネチャを維持したまま内部で `GameRepository` を呼ぶ薄いラッパーに変換。初回起動時に `Games/*.json` を自動インポートし `Games/backup/` に退避。ジャンル・WikiData・MultipleLaunch を正規化テーブルに分割。`play_history` テーブルを作成し起動時に記録。

**Tech Stack:** C# 12 / .NET 9 / WPF / `Microsoft.Data.Sqlite` / `Dapper` / `System.Text.Json`

## Global Constraints

- 対象フレームワーク: `net9.0-windows` / `LangVersion=12`
- ビルドコマンド: `dotnet build YuLauncher.sln`
- ビルド結果: 0 warnings / 0 errors
- DB配置: `%APPDATA%/YuLauncher/games.db`（Velopack 更新の影響を受けない）
- ロギング: 全て `LoggerController.LogError` / `LogInfo` / `LogWarn` を使用（AGENTS.md 準拠）
- 外部キー制約: 毎接続 `PRAGMA foreign_keys = ON` を実行
- パス区切り文字: リポジトリ境界で `jsonPath.Replace('\\', '/')` でスラッシュ統一
- `settings.toml` / `gameList.toml` はTOML維持（対象外）
- `./Games` ディレクトリは exe/html 参照先パス解決用に残置
- TDD不採用（既存プロジェクトにWPF UI単体テストなし、specで決定済み）。各タスク終了時にビルド確認と手動ステップで検証
- ローカライズ: 本計画で新規リソースキー追加なし
- 名前空間の衝突に注意（AGENTS.md / memory lesson）: `Application` / `Window` / `MessageBox` は必要に応じて完全修飾

---

## File Structure

- **新規**: `YuLauncher/Core/lib/GameRepository.cs` — 接続管理 + CRUD API + `RunMigrations` を1クラスに集約
- **修正**: `YuLauncher/Core/lib/JsonControl.cs` — `ApplicationJsonData` を `record struct` 化、`Id` 追加。`CreateExeJson` / `ReadExeJson` / `LoadJson` を `GameRepository` 呼び出しに置換。`CheckJsonData` / `CheckAppDataContent` / 手書き `Equals`/`GetHashCode` 削除
- **修正**: `YuLauncher/App.xaml.cs` — `JsonCheck()` を `InitializeDatabase()` に置換。移行ロジック追加
- **修正**: `YuLauncher/Core/lib/PageControlCreate.cs` — `File.Exists` / `File.Delete` を `GameRepository.ExistsByJsonPath` / `DeleteGameByJsonPath` に置換。`LaunchApplication` 内で `RecordPlay` 呼び出し追加
- **修正**: `YuLauncher/Core/Window/Pages/GameListPaneControl.xaml.cs` — `Directory.GetFiles` を `GameRepository.GetAll()` / `GetByGenre()` に置換。`File.Exists` を `ExistsByJsonPath` に置換
- **修正**: `YuLauncher/Core/Window/Pages/XamlCreateGameDialogInterface/Interface.cs` — `Directory.GetFiles` を `GetAll()` に置換。`ReadExeJson(jf)` を削除
- **修正**: `YuLauncher/Core/Window/Pages/Settings/General.xaml.cs` — `ExportBtn_OnClick` / `AppImportBtn_OnClick` に `games.db` コピー追加
- **修正**: `YuLauncher/YuLauncher.csproj` — `Microsoft.Data.Sqlite` / `Dapper` パッケージ参照追加

---

### Task 1: パッケージ参照の追加

**Files:**
- Modify: `YuLauncher/YuLauncher.csproj`

**Interfaces:**
- Produces: `Microsoft.Data.Sqlite` と `Dapper` がプロジェクトで利用可能

- [ ] **Step 1: csproj にパッケージ参照を追加**

`YuLauncher/YuLauncher.csproj` の `<ItemGroup>`（31-43行の PackageReference ブロック）に以下2行を追加:

```xml
      <PackageReference Include="Microsoft.Data.Sqlite" Version="9.0.*" />
      <PackageReference Include="Dapper" Version="2.1.*" />
```

追加後の `<ItemGroup>` は `HtmlAgilityPack`, `Microsoft.Web.WebView2`, `Microsoft.Data.Sqlite`, `Dapper`, `NLog`, `System.Reactive`, `System.Text.Encoding.CodePages`, `System.Text.Json`, `Tommy`, `UTF.Unknown`, `Velopack`, `WPF-UI`, `WPFLocalizeExtension` の順を想定。アルファベット順への並び替えは必須ではない。

- [ ] **Step 2: パッケージリストアとビルド確認**

Run: `dotnet build YuLauncher.sln --nologo -v q`
Expected: 0 errors, 0 warnings。初回は NuGet リストアが走る。

- [ ] **Step 3: コミット**

```bash
git add YuLauncher/YuLauncher.csproj
git commit -m "build: add Microsoft.Data.Sqlite and Dapper packages"
```

---

### Task 2: `ApplicationJsonData` の `record struct` 化

**Files:**
- Modify: `YuLauncher/Core/lib/JsonControl.cs:14-78`

**Interfaces:**
- Produces: `public record struct ApplicationJsonData { public long Id; public string FilePath; public string JsonPath; public string Name; public string? FileExtension; public string Memo; public bool? IsWebView; public bool? IsUseLog; public string Url; public string[] MultipleLaunch; public bool IsMute; public double? Volume; public string[] Genre; public Dictionary<string,string> WikiData; }`
- 削除: `Equals(ApplicationJsonData)`, `Equals(object?)`, `GetHashCode()`（record struct が自動生成）
- 影響: 呼び出し元は `data with { ... }` を既に使っているので互換

- [ ] **Step 1: `JsonControl.cs:14-78` を record struct に置換**

`YuLauncher/Core/lib/JsonControl.cs` の14-78行（`public struct ApplicationJsonData : IEquatable<ApplicationJsonData>` から閉じ波括弧まで）を以下に置換:

```csharp
    public record struct ApplicationJsonData
    {
        public long Id { get; set; }
        public string FilePath { get; set; }
        public string JsonPath { get; set; }
        public string Name { get; set; }
        public string? FileExtension { get; set; }
        public string Memo { get; set; }
        public bool? IsWebView { get; set; }
        public bool? IsUseLog { get; set; }
        public string Url { get; set; }
        public string[] MultipleLaunch { get; set; }
        public bool IsMute { get; set; }
        public double? Volume { get; set; }
        public string[] Genre { get; set; }
        public Dictionary<string, string> WikiData { get; set; }
    }
```

注意:
- `public long Id` は新規フィールド。既存JSONからのデシリアライズ時は 0（既定値）。DBから読み込んだ時のみ値が入る。
- `IEquatable<ApplicationJsonData>` 実装と手書き `Equals` / `GetHashCode` は `record struct` が自動生成するため全削除。
- 配列フィールド（`Genre`, `MultipleLaunch`）と `WikiData` の等値性は参照比較のまま（既存挙動と同じ）。

- [ ] **Step 2: ビルド確認**

Run: `dotnet build YuLauncher.sln --nologo -v q`
Expected: 0 errors, 0 warnings

- [ ] **Step 3: コミット**

```bash
git add YuLauncher/Core/lib/JsonControl.cs
git commit -m "refactor(json): convert ApplicationJsonData to record struct, add Id field"
```

---

### Task 3: `GameRepository.cs` の新設（接続管理 + RunMigrations + CRUD API）

**Files:**
- Create: `YuLauncher/Core/lib/GameRepository.cs`

**Interfaces:**
- Produces:
  - `public static string GameRepository.DbPath` — `%APPDATA%/YuLauncher/games.db`
  - `public static SqliteConnection GameRepository.CreateConnection()` — `PRAGMA foreign_keys = ON` 済み
  - `public static void GameRepository.RunMigrations()` — `schema_versions` チェック + v1 スキーマ適用
  - `public static bool GameRepository.ExistsByJsonPath(string jsonPath)` — パス正規化済み
  - `public static long GameRepository.InsertGame(ApplicationJsonData data)` — 戻り値は新規 id
  - `public static ApplicationJsonData? GameRepository.GetById(long id)`
  - `public static ApplicationJsonData? GameRepository.GetByJsonPath(string jsonPath)`
  - `public static List<ApplicationJsonData> GameRepository.GetByName(string name)`
  - `public static List<ApplicationJsonData> GameRepository.GetAll()`
  - `public static List<ApplicationJsonData> GameRepository.GetByFileExtension(string ext)`
  - `public static List<ApplicationJsonData> GameRepository.GetByGenre(string genreName)`
  - `public static List<ApplicationJsonData> GameRepository.SearchByName(string partialName)`
  - `public static string[] GameRepository.GetMultipleLaunchTargetNames(long gameId)`
  - `public static long GameRepository.GetIdByJsonPath(string jsonPath)` — 見つからない場合は -1
  - `public static void GameRepository.UpdateGame(ApplicationJsonData data)`
  - `public static void GameRepository.UpdateMemo(long gameId, string memo)`
  - `public static void GameRepository.UpdateVolume(long gameId, double volume)`
  - `public static void GameRepository.UpdateName(long gameId, string newName)`
  - `public static void GameRepository.DeleteGame(long gameId)`
  - `public static void GameRepository.DeleteGameByJsonPath(string jsonPath)`
  - `public static void GameRepository.RecordPlay(long gameId)`
  - `public static List<PlayHistoryEntry> GameRepository.GetPlayHistory(long gameId, int limit = 50)`
  - `public record PlayHistoryEntry(long Id, long GameId, DateTime PlayedAt)`

- [ ] **Step 1: ファイル作成**

`YuLauncher/Core/lib/GameRepository.cs` を新規作成。内容は以下のブロック全体:

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dapper;
using Microsoft.Data.Sqlite;
using YuLauncher.Core.lib;

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
        using var conn = CreateConnection();
        conn.Execute(
            "INSERT INTO play_history (game_id) VALUES (@gameId);",
            new { gameId });
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
```

- [ ] **Step 2: ビルド確認**

Run: `dotnet build YuLauncher.sln --nologo -v q`
Expected: 0 errors, 0 warnings

注意点:
- `IsWebViewRaw` / `IsUseLogRaw` は `long` で受け取り、`== 1` で `bool?` に変換（SQLite は INTEGER 0/1）。
- `LoadList` で `MultipleLaunch` を再帰的に取得するため N+1 クエリになるが、spec では「既存コードビハインドスタイルに合わせる」で許容。将来的に一括 JOIN に最適化可能。
- `SetMultipleLaunch` は `name` で `games` を引く。同名が複数ある場合は最初の1件（`LIMIT 1`）を参照。

- [ ] **Step 3: コミット**

```bash
git add YuLauncher/Core/lib/GameRepository.cs
git commit -m "feat(lib): add GameRepository with SQLite connection, migrations, and CRUD API"
```

---

### Task 4: `JsonControl` をリポジトリ呼び出しに置換

**Files:**
- Modify: `YuLauncher/Core/lib/JsonControl.cs:80-147`

**Interfaces:**
- Consumes: `GameRepository.ExistsByJsonPath`, `InsertGame`, `UpdateGame`, `GetByJsonPath`
- Produces: `JsonControl.ReadExeJson` / `CreateExeJson` / `LoadJson` はシグネチャ維持（`async` 削除、`ValueTask` 直接返却）

- [ ] **Step 1: `JsonControl.cs:80-147` を置換**

`YuLauncher/Core/lib/JsonControl.cs` の80行（`public static async ValueTask CreateExeJson`）から147行（ファイル末尾の閉じ波括弧）を以下に置換:

```csharp
    public static ValueTask CreateExeJson(string path, ApplicationJsonData applicationJsonData)
    {
        var dataWith = applicationJsonData with { JsonPath = path };
        if (GameRepository.ExistsByJsonPath(path))
            GameRepository.UpdateGame(dataWith);
        else
            GameRepository.InsertGame(dataWith);
        return ValueTask.CompletedTask;
    }

    public static ValueTask<ApplicationJsonData> ReadExeJson(string path)
    {
        return new ValueTask<ApplicationJsonData>(GameRepository.GetByJsonPath(path) ?? default);
    }

    public static ApplicationJsonData LoadJson(string path)
    {
        return GameRepository.GetByJsonPath(path) ?? default;
    }
}
```

注意:
- `async` キーワード削除（CS1998 警告回避 + 不要なステートマシン確保防止）。
- `Task.FromResult` は既存アンチパターン（memory lesson）なので使用しない。
- `CheckAppDataContent` / `CheckJsonData` は削除（正規化済データなので不要）。
- 既存の `using System.Text.Encodings.Web` / `System.Text.Json` / `System.Text.Json.Serialization` / `System.Text.Unicode` / `System.IO` は未使用になるが、コンパイルには影響しない。後続タスクで整理可能。

- [ ] **Step 2: ビルド確認**

Run: `dotnet build YuLauncher.sln --nologo -v q`
Expected: 0 errors, 0 warnings

- [ ] **Step 3: コミット**

```bash
git add YuLauncher/Core/lib/JsonControl.cs
git commit -m "refactor(json): replace JsonControl body with GameRepository calls"
```

---

### Task 5: `App.xaml.cs` の移行ロジック追加

**Files:**
- Modify: `YuLauncher/App.xaml.cs:30-50`（`Application_Startup`）
- Modify: `YuLauncher/App.xaml.cs:145-154`（`JsonCheck` 本体を置換）

**Interfaces:**
- Consumes: `GameRepository.RunMigrations`, `GameRepository.ExistsByJsonPath`, `GameRepository.InsertGame`
- Produces: `App.InitializeDatabase()`, `App.ImportExistingJsonFiles()`

- [ ] **Step 1: using 追加**

`YuLauncher/App.xaml.cs` の using ブロック（1-19行付近）に以下を追加:

```csharp
using System.Text.Json;
```

- [ ] **Step 2: `Application_Startup` 内の `JsonCheck()` 呼び出しを `InitializeDatabase()` に置換**

40行目の `await JsonCheck();` を以下に置換:

```csharp
                await InitializeDatabase();
```

- [ ] **Step 3: `JsonCheck` メソッドを `InitializeDatabase` と `ImportExistingJsonFiles` に置換**

`App.xaml.cs:145-154`（`private static async ValueTask JsonCheck()` から閉じ波括弧まで）を以下に置換:

```csharp
        private static async ValueTask InitializeDatabase()
        {
            try
            {
                string appDataDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "YuLauncher");
                if (!Directory.Exists(appDataDir))
                    Directory.CreateDirectory(appDataDir);

                GameRepository.RunMigrations();
                LoggerController.LogInfo("Database migrations complete");

                await ImportExistingJsonFiles();
            }
            catch (Exception e)
            {
                LoggerController.LogError($"InitializeDatabase failed: {e}");
            }
        }

        private static async ValueTask ImportExistingJsonFiles()
        {
            var jsonFiles = Directory.GetFiles("./Games", "*.json");
            if (jsonFiles.Length == 0) return;

            var importedFiles = new List<string>();
            foreach (var file in jsonFiles)
            {
                try
                {
                    string json = await File.ReadAllTextAsync(file);
                    var data = JsonSerializer.Deserialize<JsonControl.ApplicationJsonData>(json);
                    if (data.Name == null) continue;

                    // CheckJsonData と同等の正規化（null フィールド補完、Genre デフォルト判定）
                    data = data with
                    {
                        FilePath       = data.FilePath ?? "",
                        JsonPath       = data.JsonPath ?? "",
                        Name           = data.Name ?? "",
                        FileExtension  = data.FileExtension ?? "Unknown",
                        Memo           = data.Memo ?? "",
                        IsWebView      = data.IsWebView ?? false,
                        IsUseLog       = data.IsUseLog ?? false,
                        Url            = data.Url ?? "",
                        MultipleLaunch = data.MultipleLaunch ?? [],
                        WikiData       = data.WikiData ?? new(),
                        Genre          = data.Genre ?? (data.FileExtension switch {
                                            "exe"      => ["Application"],
                                            "web"      => ["WebSite"],
                                            "WebGame"  => ["WebGame"],
                                            "WebSaver" => ["WebSaver"],
                                            _          => ["Unknown"],
                                        }),
                    };

                    string relPath = Path.GetFileName(file);
                    var dataWithPath = data with { JsonPath = $"./Games/{relPath}" };

                    if (!GameRepository.ExistsByJsonPath(dataWithPath.JsonPath))
                    {
                        GameRepository.InsertGame(dataWithPath);
                        importedFiles.Add(file);
                    }
                }
                catch (Exception e)
                {
                    LoggerController.LogError($"Failed to import {file}: {e}");
                    // 失敗したファイルは importedFiles に入れない → backup/ に移動されず残る
                }
            }

            if (importedFiles.Count > 0)
            {
                LoggerController.LogInfo($"Imported {importedFiles.Count} games from JSON to SQLite");

                string backupDir = Path.Combine("./Games", "backup");
                if (!Directory.Exists(backupDir))
                    Directory.CreateDirectory(backupDir);

                foreach (var file in importedFiles)
                {
                    string dest = Path.Combine(backupDir, Path.GetFileName(file));
                    File.Move(file, dest, overwrite: true);
                }

                LoggerController.LogInfo($"Moved {importedFiles.Count} imported JSON files to {backupDir}");
            }
        }
```

注意:
- `InitializeDatabase` は `try-catch` で全体を囲み、失敗時もアプリ起動を継続。
- インポート失敗のファイルは `importedFiles` に入らないため `backup/` に移動されず、元位置に残る。
- `Genre` のデフォルト判定は既存 `CheckJsonData`（`JsonControl.cs:136-142`）のロジックを移植。

- [ ] **Step 4: ビルド確認**

Run: `dotnet build YuLauncher.sln --nologo -v q`
Expected: 0 errors, 0 warnings

- [ ] **Step 5: コミット**

```bash
git add YuLauncher/App.xaml.cs
git commit -m "feat(app): replace JsonCheck with InitializeDatabase and JSON import logic"
```

---

### Task 6: `GameListPaneControl.xaml.cs` の呼び出し元置換

**Files:**
- Modify: `YuLauncher/Core/Window/Pages/GameListPaneControl.xaml.cs:73-176`（`LoadGenre` / `LoadAllGames` / `LoadGamesByGenre`）
- Modify: `YuLauncher/Core/Window/Pages/GameListPaneControl.xaml.cs:395,410,425,440`（`File.Exists` ガード4箇所）

**Interfaces:**
- Consumes: `GameRepository.GetAll`, `GetByGenre`, `ExistsByJsonPath`

- [ ] **Step 1: `LoadGenre` メソッド全体を置換**

`GameListPaneControl.xaml.cs:73-117`（`private async ValueTask LoadGenre()` から閉じ波括弧まで）を以下に置換:

```csharp
    private async ValueTask LoadGenre()
    {
        List<string> genreList = new();

        var games = await Task.Run(() => GameRepository.GetAll());
        foreach (var data in games)
        {
            if (!MatchesFilter(data)) continue;
            data.Genre?.ToList().ForEach(x =>
            {
                if (!genreList.Contains(x))
                {
                    genreList.Add(x);
                }
            });
        }

        GenreComboBox.Items.OfType<ComboBoxItem>()
            .Where(x => x != GenreAllComboBoxItem)
            .ToList()
            .ForEach(x => GenreComboBox.Items.Remove(x));

        genreList.ForEach(x =>
        {
            var genre = GenreComboBox.Items.OfType<ComboBoxItem>().FirstOrDefault(item => item.Content?.ToString() == x);
            if (genre != null && (string)genre.Content == x) return;
            ComboBoxItem comboBoxItem = new()
            {
                Content = x
            };
            comboBoxItem.Selected += async (sender, args) =>
            {
                GenreComboBox.SelectedItem = comboBoxItem;
                await LoadGamesByGenre(x);
            };
            GenreComboBox.Items.Add(comboBoxItem);
        });
    }
```

- [ ] **Step 2: `LoadAllGames` メソッド全体を置換**

`GameListPaneControl.xaml.cs:119-146` を以下に置換:

```csharp
    private async Task LoadAllGames()
    {
        _allGames.Clear();
        _gameItems.Clear();

        var games = await Task.Run(() => GameRepository.GetAll());
        foreach (var data in games)
        {
            try
            {
                if (!MatchesFilter(data)) continue;
                var item = CreateGameListItem(data);
                _allGames.Add(item);
                _gameItems.Add(item);
            }
            catch (Exception ex)
            {
                LoggerController.LogError($"{ex}");
                LoggerController.LogError("An I/O error occurred: " + ex.Message);
            }
        }
    }
```

- [ ] **Step 3: `LoadGamesByGenre` メソッド全体を置換**

`GameListPaneControl.xaml.cs:148-176` を以下に置換:

```csharp
    private async Task LoadGamesByGenre(string genre)
    {
        _allGames.Clear();
        _gameItems.Clear();

        var games = await Task.Run(() => GameRepository.GetByGenre(genre));
        foreach (var data in games)
        {
            try
            {
                if (!MatchesFilter(data)) continue;
                var item = CreateGameListItem(data);
                _allGames.Add(item);
                _gameItems.Add(item);
            }
            catch (Exception ex)
            {
                LoggerController.LogError($"{ex}");
                LoggerController.LogError("An I/O error occurred: " + ex.Message);
            }
        }
    }
```

- [ ] **Step 4: `File.Exists(data.JsonPath)` を4箇所置換**

395, 410, 425, 440行の `if (!File.Exists(data.JsonPath)) return;` を以下に置換:

```csharp
        if (!GameRepository.ExistsByJsonPath(data.JsonPath)) return;
```

- [ ] **Step 5: ビルド確認**

Run: `dotnet build YuLauncher.sln --nologo -v q`
Expected: 0 errors, 0 warnings

- [ ] **Step 6: コミット**

```bash
git add YuLauncher/Core/Window/Pages/GameListPaneControl.xaml.cs
git commit -m "refactor(pages): replace Directory.GetFiles and File.Exists with GameRepository calls"
```

---

### Task 7: `PageControlCreate.cs` の呼び出し元置換と `RecordPlay` 追加

**Files:**
- Modify: `YuLauncher/Core/lib/PageControlCreate.cs:68,73,75,98,117`
- Modify: `YuLauncher/Core/lib/PageControlCreate.cs:294-313`（`LaunchApplication` に `RecordPlay` 追加）

**Interfaces:**
- Consumes: `GameRepository.DeleteGameByJsonPath`, `ExistsByJsonPath`, `GetIdByJsonPath`, `RecordPlay`

- [ ] **Step 1: 68行 `File.Delete(data.JsonPath)` を置換**

```csharp
                            GameRepository.DeleteGameByJsonPath(data.JsonPath);
```

- [ ] **Step 2: 73行 `File.Exists(data.JsonPath)` を置換**

```csharp
                            if (GameRepository.ExistsByJsonPath(data.JsonPath))
```

- [ ] **Step 3: 75行 `File.Delete(data.JsonPath)` を置換**

```csharp
                                GameRepository.DeleteGameByJsonPath(data.JsonPath);
```

- [ ] **Step 4: 98行 `File.Exists(data.JsonPath)` を置換**

```csharp
                    if (!GameRepository.ExistsByJsonPath(data.JsonPath)) return;
```

- [ ] **Step 5: 117行 `File.Exists(data.JsonPath)` を置換**

98行と同じ置換:

```csharp
                    if (!GameRepository.ExistsByJsonPath(data.JsonPath)) return;
```

- [ ] **Step 6: `LaunchApplication` 内で `RecordPlay` 呼び出し追加**

`PageControlCreate.cs:294-313`（`internal static async Task LaunchApplication`）を以下に置換:

```csharp
    internal static async Task LaunchApplication(JsonControl.ApplicationJsonData data)
    {
        switch (data.FileExtension)
        {
            case "exe":
                await LaunchExe(data);
                GameRepository.RecordPlay(data.Id);
                break;
            case "web":
                LaunchWeb(data);
                GameRepository.RecordPlay(data.Id);
                break;
            case "WebGame":
                new GameWindow(data.Url, data.JsonPath).Show();
                GameRepository.RecordPlay(data.Id);
                break;
            case "WebSaver":
                new WebSaverWindow.WebSaverWindow(data.Name, data).Show();
                GameRepository.RecordPlay(data.Id);
                break;
            case "":
                break;
        }
    }
```

注意:
- `data.Id` は `GetAll` / `GetByJsonPath` 等の読み込み系APIがDBから取得した際に設定済み。`LaunchApplication` に渡る `data` は常にDB経由で取得されているため、`Id != 0` を前提とする。
- 万が一 `Id == 0`（未永続化データ）の場合、`RecordPlay(0)` は FK 制約違反で例外になる。これは既存の try-catch で処理される（呼び出し元 `GameButtonShow` の Click ハンドラと `GameListPaneControl.LaunchAsync` が try-catch 持ち）。

- [ ] **Step 7: ビルド確認**

Run: `dotnet build YuLauncher.sln --nologo -v q`
Expected: 0 errors, 0 warnings

- [ ] **Step 8: コミット**

```bash
git add YuLauncher/Core/lib/PageControlCreate.cs
git commit -m "refactor(lib): replace File.Exists/Delete with GameRepository, add RecordPlay on launch"
```

---

### Task 8: `Interface.cs` の呼び出し元置換

**Files:**
- Modify: `YuLauncher/Core/Window/Pages/XamlCreateGameDialogInterface/Interface.cs:42-75`（`LoadMultipleLaunchCandidatesAsync`）

**Interfaces:**
- Consumes: `GameRepository.GetAll`

- [ ] **Step 1: `LoadMultipleLaunchCandidatesAsync` を置換**

`Interface.cs:42-75` を以下に置換:

```csharp
    protected async Task LoadMultipleLaunchCandidatesAsync()
    {
        try
        {
            var allGames = await Task.Run(() => GameRepository.GetAll());
            var existing = new HashSet<string>(
                Candidates.Select(c => c.Name),
                StringComparer.Ordinal
            );

            foreach (var candidate in allGames)
            {
                if (candidate.Name == Data.Name) continue;
                if (existing.Contains(candidate.Name)) continue;

                Candidates.Add(new MultipleLaunchCandidate(
                    candidate.FilePath,
                    candidate.Name,
                    Data.MultipleLaunch.Contains(candidate.Name)
                ));
                existing.Add(candidate.Name);
            }
        }
        catch (IndexOutOfRangeException ex)
        {
            LoggerController.LogError(ex.Message);
            await ShowOldSystemErrorAsync();
        }
        catch (Exception exception)
        {
            LoggerController.LogError($"{exception}");
        }
    }
```

注意:
- `Directory.GetFiles("./Games", "*.json")` を `GameRepository.GetAll()` に置換。
- ループ変数が `jf`（文字列パス）から `candidate`（`ApplicationJsonData`）に変更。`ReadExeJson(jf)` 呼び出しは不要になり、`candidate.FilePath` と `candidate.Name` を直接使用。

- [ ] **Step 2: ビルド確認**

Run: `dotnet build YuLauncher.sln --nologo -v q`
Expected: 0 errors, 0 warnings

- [ ] **Step 3: コミット**

```bash
git add YuLauncher/Core/Window/Pages/XamlCreateGameDialogInterface/Interface.cs
git commit -m "refactor(dialog): replace Directory.GetFiles with GameRepository.GetAll in LoadMultipleLaunchCandidatesAsync"
```

---

### Task 9: `General.xaml.cs` のバックアップ/インポート機能更新

**Files:**
- Modify: `YuLauncher/Core/Window/Pages/Settings/General.xaml.cs:54-116`（`ExportBtn_OnClick`）
- Modify: `YuLauncher/Core/Window/Pages/Settings/General.xaml.cs:118-176`（`AppImportBtn_OnClick`）

**Interfaces:**
- Consumes: `GameRepository.DbPath`

- [ ] **Step 1: using 追加**

`General.xaml.cs` の using ブロックに以下を追加:

```csharp
using YuLauncher.Core.lib;
```

既に `using System.IO;` 等がある前提。`GameRepository` は `YuLauncher.Core.lib` 名前空間。

- [ ] **Step 2: `ExportBtn_OnClick` に `games.db` コピーを追加**

`General.xaml.cs:54-116`（`ExportBtn_OnClick`）の末尾付近、`MessageBox.Show(LocalizeControl.GetLocalize<string>("SimpleCompleted"));` の直前に以下を挿入:

```csharp
            string dbPath = GameRepository.DbPath;
            if (File.Exists(dbPath))
            {
                string destDb = Path.Combine(dialog.FolderName, "games.db");
                File.Copy(dbPath, destDb, true);
            }
            else
            {
                LoggerController.LogError("games.db not found at: " + dbPath);
            }
```

注意: `./Games` のJSONコピー処理（66-77行）は残置。将来的に空になるが、`./Games` 自体は exe/html 参照先として残るため。

- [ ] **Step 3: `AppImportBtn_OnClick` に `games.db` コピーを追加**

`General.xaml.cs:118-176`（`AppImportBtn_OnClick`）に以下を追加。`settings.toml` コピー処理（156-164行）の直前あたりに挿入:

```csharp
            string srcDb = Path.Combine(dialog.FolderName, "games.db");
            if (File.Exists(srcDb))
            {
                string destDb = GameRepository.DbPath;
                Directory.CreateDirectory(Path.GetDirectoryName(destDb)!);
                File.Copy(srcDb, destDb, true);
                LoggerController.LogInfo("Imported games.db from " + srcDb);
            }
            else
            {
                MessageBox.Show("games.db not found in selected folder");
            }
```

注意:
- `games.db` がインポート元に存在しない場合は `MessageBox` で警告。既存の「Games Folder not found」等と同じパターン。
- 旧JSON形式からのインポートは `App.xaml.cs` の `ImportExistingJsonFiles` が初回起動時に処理するため、ここでは `games.db` コピーのみ。

- [ ] **Step 4: ビルド確認**

Run: `dotnet build YuLauncher.sln --nologo -v q`
Expected: 0 errors, 0 warnings

- [ ] **Step 5: コミット**

```bash
git add YuLauncher/Core/Window/Pages/Settings/General.xaml.cs
git commit -m "feat(settings): add games.db copy to export/import backup functions"
```

---

### Task 10: ビルド検証と手動動作確認

**Files:**
- なし（検証のみ）

- [ ] **Step 1: フルビルド**

Run: `dotnet build YuLauncher.sln --nologo -v q`
Expected: 0 errors, 0 warnings

- [ ] **Step 2: ユーザーに手動起動を依頼**

ユーザーにアプリを手動起動してもらう（hub経由の起動は `YuLauncher.exe` パス解決に失敗する既知の問題のため）。

依頼内容:
1. 既存 `Games/*.json` がある状態でアプリ起動
2. 起動後、`%APPDATA%/YuLauncher/games.db` が作成されたか確認
3. `Games/*.json` が `Games/backup/` に移動されたか確認
4. ゲームリストが表示されるか確認

- [ ] **Step 3: 動作確認項目（ユーザーに実施依頼）**

以下を順番に確認:

- [ ] 初回起動時に `games.db` が `%APPDATA%/YuLauncher/` に作成される
- [ ] 既存 `Games/*.json` がDBにインポートされる
- [ ] インポート後、`Games/*.json` が `Games/backup/` に移動される
- [ ] ゲームリストが表示される（DBから読み込み）
- [ ] ジャンルフィルタが動作する（`GetByGenre`）
- [ ] 検索ボックスが動作する（`SearchByName`）
- [ ] ゲーム起動が動作する
- [ ] プロパティダイアログが開く（`ExistsByJsonPath` ガード）
- [ ] メモウィンドウが開く
- [ ] Wiki管理ウィンドウが開く
- [ ] ジャンル管理ウィンドウが開く
- [ ] ゲーム削除がDBから反映される（`DeleteGameByJsonPath`）
- [ ] ゲーム作成がDBに反映される（`CreateGameDialog` → `InsertGame`）
- [ ] 設定のエクスポートに `games.db` が含まれる
- [ ] 設定のインポートで `games.db` が復元される
- [ ] MultipleLaunch が動作する（`game_multiple_launch` テーブル経由）
- [ ] 起動時に `play_history` にレコードが挿入される
- [ ] パス区切り文字の正規化が動作する（`./Games\hoge.json` と `./Games/hoge.json` が同一レコードとして扱われる）
- [ ] 同名ゲームが複数作成可能（`name` に UNIQUE 制約なし）
- [ ] `original_json_path` 重複時は `ExistsByJsonPath` で事前検知され、INSERT に進まない
- [ ] `data.Id` が `GetAll` / `GetByJsonPath` 戻り値で正しく設定されている
- [ ] `MultipleLaunch` で対象ゲームが0件の場合、スキップされる（`GetByName` が空リストを返す）

- [ ] **Step 4: 最終コミット（必要な場合）**

動作確認で発見された修正があればコミット。全項目パスなら完了。

```bash
git log --oneline -20
```

期待されるコミット履歴:
1. `build: add Microsoft.Data.Sqlite and Dapper packages`
2. `refactor(json): convert ApplicationJsonData to record struct, add Id field`
3. `feat(lib): add GameRepository with SQLite connection, migrations, and CRUD API`
4. `refactor(json): replace JsonControl body with GameRepository calls`
5. `feat(app): replace JsonCheck with InitializeDatabase and JSON import logic`
6. `refactor(pages): replace Directory.GetFiles and File.Exists with GameRepository calls`
7. `refactor(lib): replace File.Exists/Delete with GameRepository, add RecordPlay on launch`
8. `refactor(dialog): replace Directory.GetFiles with GameRepository.GetAll in LoadMultipleLaunchCandidatesAsync`
9. `feat(settings): add games.db copy to export/import backup functions`
