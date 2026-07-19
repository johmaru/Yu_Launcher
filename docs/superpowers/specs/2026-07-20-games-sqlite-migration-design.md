# ゲームデータの SQLite 移行 — 設計ドキュメント

**日付**: 2026-07-20
**対象**: ゲームデータ管理（現状 `Games/*.json`）の SQLite 化
**アプローチ**: A（リポジトリ層新設・既存API互換）

## 背景

現状のゲームデータは `Games/*.json`（1エントリ=1ファイル）で管理されており、`JsonControl.ApplicationJsonData` 構造体にシリアライズしてアクセスする。

### 現状の課題

1. **N+1読み込み**: `GameListPaneControl.xaml.cs:85,131,160` は `Directory.GetFiles` で全ファイル列挙後、1件ずつ `JsonControl.ReadExeJson` で個別読み込み。ジャンル絞り込みのたびに全JSON再読込。
2. **ファイルガードの重複**: `File.Exists(data.JsonPath)` が7箇所、`File.Delete(data.JsonPath)` が2箇所。各操作ごとにファイルIO発生。
3. **参照整合性なし**: `MultipleLaunch`（他ゲームへの参照）が名前配列で、参照先ゲーム削除時に孤立レコードが残る。
4. **部分更新不可**: `GenreManageWindow` / `WikiDataManageWindow` で1ジャンル編集するだけで `ApplicationJsonData` 全体を再シリアライズしてファイル上書き。
5. **履歴機能未実装**: `GameHistoryPage.xaml` が空。起動履歴を蓄積する土台なし。

### 対象外

- `settings.toml` — TOML維持（数キーのみ、RDB過剰スペック）
- `gameList.toml` — 実質 `testgame="test"` のみ。不要だが本作業外
- `GameHistoryPage.xaml` のUI実装 — 別タスク。本作業では `play_history` テーブル作成と記録ロジックのみ

## スコープ

### 対象

- 新規: `YuLauncher/Core/lib/GameRepository.cs`（`RunMigrations` 含む。接続管理とマイグレーションを1クラスに集約）
- 修正: `YuLauncher/Core/lib/JsonControl.cs`
- 修正: `YuLauncher/App.xaml.cs`
- 修正: `YuLauncher/Core/lib/PageControlCreate.cs`
- 修正: `YuLauncher/Core/Window/Pages/GameListPaneControl.xaml.cs`
- 修正: `YuLauncher/Core/Window/Pages/XamlCreateGameDialogInterface/Interface.cs`
- 修正: `YuLauncher/Core/Window/Pages/Settings/General.xaml.cs`
- 修正: `YuLauncher/YuLauncher.csproj`（パッケージ参照追加）

### 非目標

- `JsonControl` → `GameRepository` への完全リネーム（別タスク）
- `GameHistoryPage.xaml` のUI実装（別タスク）
- `settings.toml` の SQLite化
- MVVM / ViewModel パターンの導入
- バックアップ機能の全面リライト（`games.db` のコピー追加のみ）

## 技術選定

| 要件 | 選定 | 理由 |
|---|---|---|
| SQLite ドライバ | `Microsoft.Data.Sqlite` | 標準的。EF Core は過剰スペック |
| O/Rマッパー | `Dapper` | 軽量。SQL直接記述スタイルが既存コードに合う |
| DB配置場所 | `%APPDATA%/YuLauncher/games.db` | Windows 標準のユーザーデータ配置。Velopack 更新の影響を受けない |
| スキーマ管理 | `schema_versions` テーブル + 段階的適用 | 将来のカラム追加時に v2, v3 を積める |
| 正規化 | `games` / `genres` / `game_genres` / `wiki_data` / `game_multiple_launch` / `play_history` | ジャンル・WikiData・MultipleLaunch を完全正規化 |

## スキーマ

配置場所: `%APPDATA%/YuLauncher/games.db`

```sql
-- マイグレーション管理
CREATE TABLE schema_versions (
    version INTEGER PRIMARY KEY,
    applied_at TEXT NOT NULL DEFAULT (datetime('now'))
);

-- ゲーム本体
CREATE TABLE games (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    name TEXT NOT NULL,
    file_path TEXT NOT NULL DEFAULT '',
    original_json_path TEXT NOT NULL UNIQUE DEFAULT '',  -- 移行元の Games/{name}.json。正規化済み（バックスラッシュ→スラッシュ）。既存API互換用。UNIQUE で重複レコード防止
    file_extension TEXT NOT NULL DEFAULT '',
    memo TEXT NOT NULL DEFAULT '',
    is_web_view INTEGER NOT NULL DEFAULT 0,       -- BOOL (0/1)
    is_use_log INTEGER NOT NULL DEFAULT 0,
    url TEXT NOT NULL DEFAULT '',
    is_mute INTEGER NOT NULL DEFAULT 0,
    volume REAL,
    created_at TEXT NOT NULL DEFAULT (datetime('now')),
    updated_at TEXT NOT NULL DEFAULT (datetime('now'))
);
-- original_json_path の UNIQUE 制約が自動的にインデックスを生成する。name は重複許容（同名ゲームが別ファイルとして存在し得るため）。name で引く場合は GetByName が List で返す（後述）
CREATE INDEX idx_games_name ON games(name);

-- ジャンル
CREATE TABLE genres (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    name TEXT NOT NULL UNIQUE
);

-- ゲーム ↔ ジャンル (多対多)
CREATE TABLE game_genres (
    game_id INTEGER NOT NULL,
    genre_id INTEGER NOT NULL,
    PRIMARY KEY (game_id, genre_id),
    FOREIGN KEY (game_id) REFERENCES games(id) ON DELETE CASCADE,
    FOREIGN KEY (genre_id) REFERENCES genres(id) ON DELETE CASCADE
);

-- WikiData
CREATE TABLE wiki_data (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    game_id INTEGER NOT NULL,
    key TEXT NOT NULL,
    value TEXT NOT NULL,
    UNIQUE (game_id, key),
    FOREIGN KEY (game_id) REFERENCES games(id) ON DELETE CASCADE
);

-- MultipleLaunch
CREATE TABLE game_multiple_launch (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    game_id INTEGER NOT NULL,
    target_game_id INTEGER NOT NULL,
    sort_order INTEGER NOT NULL DEFAULT 0,
    FOREIGN KEY (game_id) REFERENCES games(id) ON DELETE CASCADE,
    FOREIGN KEY (target_game_id) REFERENCES games(id) ON DELETE CASCADE
);

-- 起動履歴
CREATE TABLE play_history (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    game_id INTEGER NOT NULL,
    played_at TEXT NOT NULL DEFAULT (datetime('now')),
    FOREIGN KEY (game_id) REFERENCES games(id) ON DELETE CASCADE
);
CREATE INDEX idx_play_history_game_id ON play_history(game_id);
CREATE INDEX idx_play_history_played_at ON play_history(played_at);
```

### 型マッピング

| C# 側 | SQLite 側 | 備考 |
|---|---|---|
| `string` | TEXT | NULL不可は `NOT NULL DEFAULT ''` |
| `bool?` | INTEGER | 0/1。NULL許容はそのまま |
| `double?` | REAL | NULL許容 |
| `string[]` (Genre, MultipleLaunch) | 別テーブル | 正規化 |
| `Dictionary<string,string>` (WikiData) | 別テーブル | 正規化 |

### 設計メモ

1. **`original_json_path`**: 移行時に `./Games/{name}.json` を記録。`JsonControl.ReadExeJson(path)` はこれをキーにSELECT。新規作成時は `{name}.json` から逆算。将来的に `path` ベースAPIを廃止したらカラム削除可。
2. **BOOL表現**: SQLite は BOOL 型を持たない。`INTEGER` 0/1 で運用。Dapper の `bool` マッピングが効く。
3. **外部キー制約**: `PRAGMA foreign_keys = ON` を接続時に必ず実行（SQLite はデフォルトOFF）。
4. **`MultipleLaunch` の `sort_order`**: 現状の配列順序を保持。起動順序が意味を持つ可能性があるため。
5. **`genres` テーブルがマスタ**: `GenreManageWindow` は `genres` の追加/削除/リネームを直接操作できる。ゲーム側のジャンル編集は `game_genres` の操作。

## リポジトリIF

`YuLauncher/Core/lib/GameRepository.cs` を新設。Dapper ベース。

### 接続管理

```csharp
public static class GameRepository
{
    public static string DbPath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                     "YuLauncher", "games.db");

    public static SqliteConnection CreateConnection()
    {
        var conn = new SqliteConnection($"Data Source={DbPath}");
        conn.Open();
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "PRAGMA foreign_keys = ON;";
            cmd.ExecuteNonQuery();
        }
        return conn;
    }
}
```

- 接続都度 `Open`/`Close`。DbContext のような長期接続は持たない（既存コードビハインドスタイルに合わせる）。
- `PRAGMA foreign_keys = ON` を毎接続実行（SQLite の接続単位設定）。
- `DbPath` は `%APPDATA%/YuLauncher/games.db`。初回起動時にディレクトリ作成。

### 公開API

```csharp
public static class GameRepository
{
    // ===== 接続 =====
    public static SqliteConnection CreateConnection();  // PRAGMA foreign_keys = ON 済み

    // ===== マイグレーション =====
    public static void RunMigrations();  // schema_versions チェック＋適用

    // ===== 存在チェック（File.Exists 置換用） =====
    public static bool ExistsByJsonPath(string jsonPath);

    // ===== CREATE =====
    public static long InsertGame(ApplicationJsonData data);  // genres/wiki/multiple_launch含めて1トランザクション。戻り値は新規採番された id

    // ===== READ =====
    public static ApplicationJsonData? GetById(long id);
    public static ApplicationJsonData? GetByJsonPath(string jsonPath);  // 既存API互換。引数のパス区切り文字は正規化して比較（後述）
    public static List<ApplicationJsonData> GetByName(string name);         // 同名が複数存在し得るため List で返す。MultipleLaunch参照解決用。空リスト可能性あり
    public static List<ApplicationJsonData> GetAll();
    public static List<ApplicationJsonData> GetByFileExtension(string ext);
    public static List<ApplicationJsonData> GetByGenre(string genreName);
    public static List<ApplicationJsonData> SearchByName(string partialName);
    public static string[] GetMultipleLaunchTargetNames(long gameId);  // A案：名前配列で返す

    // ===== Id 解決ヘルパー =====
    // 既存呼び出し元が ApplicationJsonData を持っているが Id を持っていない場合のため。
    // 内部で GetByJsonPath(data.JsonPath) を呼んで Id を取り出す。見つからない場合は -1 を返す。
    public static long GetIdByJsonPath(string jsonPath);

    // ===== UPDATE（全体） =====
    public static void UpdateGame(ApplicationJsonData data);  // genres/wiki/multiple_launch含めて1トランザクション

    // ===== UPDATE（部分） =====
    public static void UpdateMemo(long gameId, string memo);
    public static void UpdateVolume(long gameId, double volume);
    public static void UpdateName(long gameId, string newName);

    // ===== DELETE =====
    public static void DeleteGame(long gameId);  // CASCADE で game_genres/wiki/multiple_launch/history も削除
    public static void DeleteGameByJsonPath(string jsonPath);  // File.Delete 置換用

    // ===== 履歴 =====
    public static void RecordPlay(long gameId);
    public static List<PlayHistoryEntry> GetPlayHistory(long gameId, int limit = 50);
}

public record PlayHistoryEntry(long Id, long GameId, DateTime PlayedAt);
```

### `ApplicationJsonData` の `record struct` 化

現状は `public struct ApplicationJsonData : IEquatable<ApplicationJsonData>`（`JsonControl.cs:14`）。手書きの `Equals` / `GetHashCode`（38-77行、約40行）を実装。

**変更**: `public record struct ApplicationJsonData` に変更し、手書き実装を削除。加えて `public long Id` フィールドを新設。

**根拠**: `record struct` が `Equals` / `GetHashCode` / `==` / `!=` / `ToString` を自動生成するため。手書き実装は約40行のボイラープレートで、バグの温床。

**`with` 式についての補足**: C# 10 以降、`struct` でも `with` 式は使用可能であり、現状の `JsonControl.cs:124` の `CheckJsonData` で既に動いている。したがって `record struct` 化の理由は `with` 式のためではなく、手書き `Equals`/`GetHashCode` の削除が目的。

**配列等値性の注意**: 既存の手書き `Equals` は `Genre`/`WikiData`/`MultipleLaunch`（配列・Dict）に対して `==` を使っており、これは参照比較。`record struct` 化でも配列の等値性は参照比較のままで挙動変わらず、デグレなし。ただし将来的に値比較が必要になった場合は `SequenceEqual` 等の自前実装が必要。

**`Id` フィールドの扱い**: `ApplicationJsonData.Id` は SQLite の `games.id` を保持。既存JSONからのデシリアライズ時は 0（既定値）。`GameRepository.GetByJsonPath` / `GetAll` / `GetById` 等の読み込み系APIがDBから取得した際にセット。呼び出し元が `data.Id` で `RecordPlay` / `UpdateMemo` / `DeleteGame` 等 `*ByGameId` 系APIに渡す。`GetIdByJsonPath(data.JsonPath)` は `data` から `Id` を取り出すヘルパー。新規作成時（`CreateGameDialog`）は `InsertGame` の戻り値（採番されたid）を使うか、直後に `GetByJsonPath` で取得し直す。

### パス区切り文字の正規化

`CreateGameDialog.xaml.cs:136,171` が `JsonPath = $"{FileControl.Main.Directory}\\{name}.json"` で `./Games\name.json`（バックスラッシュ）を生成する一方、移行ロジックは `./Games/{name}.json`（スラッシュ）を書き込む。SQLiteの TEXT 比較はバイト単位なので、この不一致は `ExistsByJsonPath` / `GetByJsonPath` の miss を引き起こす。

**対応**: リポジトリ境界で `original_json_path` を正規化。`GetByJsonPath` / `ExistsByJsonPath` / `DeleteGameByJsonPath` / `InsertGame` の各メソッドは引数の `jsonPath` を受け取った時点で `jsonPath.Replace('\\', '/')` でスラッシュ統一してから SQL に渡す。呼び出し元の `JsonPath` 生成コードは変更しない（後方互換）。

### `MultipleLaunch` のデータ型（A案）

`ApplicationJsonData.MultipleLaunch` は `string[]`（ゲーム名配列）のまま維持。リポジトリ内部で `game_multiple_launch` テーブルを JOIN し、`target_game_id` から `games.name` を引いて名前配列に詰め替える。`sort_order` 順に取得。

既存呼び出し元（`GameListPaneControl.xaml.cs:357-365`, `PageControlCreate.cs:430-438`）のコードはそのまま動く。

### トランザクション方針

- 単一操作は自動コミット
- `UpdateGame` は内部で private メソッド `SetGenres` / `SetWikiData` / `SetMultipleLaunch` を呼び、「全削除→全挿入」を1トランザクションで実行。これらは `GameRepository` の private メソッド（外部非公開）
- `InsertGame` はゲーム本体INSERT + 初期ジャンル/Wiki/MultipleLaunch を1トランザクションで
- `updated_at` はアプリ側で明示的に `datetime('now')` をセット（トリガー不使用）。`UpdateGame` の SQL で `updated_at = datetime('now')` を含める
- `InsertGame` は `data.JsonPath` を `original_json_path` カラムに挿入。`ApplicationJsonData` に `original_json_path` フィールドは無いため、`JsonPath` から直接マッピング

### 非同期の扱い

既存 `JsonControl` の `ReadExeJson` / `CreateExeJson` は `async ValueTask` シグネチャ。リポジトリ内部は同期（Dapperの同期API）。

**選定**: `async` キーワードを外し、`ValueTask<T>` を直接 `new ValueTask<T>(...)` で返す（A案）。`async` を残すと内部に `await` が無く CS1998 警告が出る上、不要なステートマシンを確保する。`Task.FromResult` で包むのは既存アンチパターン（memory lesson）。呼び出し元の `await JsonControl.ReadExeJson(...)` はそのまま動く（`ValueTask<T>` は `await` 可能）。

```csharp
public static class JsonControl
{
    public static ValueTask<ApplicationJsonData> ReadExeJson(string path)
    {
        return new ValueTask<ApplicationJsonData>(GameRepository.GetByJsonPath(path) ?? default);
    }

    public static ValueTask CreateExeJson(string path, ApplicationJsonData data)
    {
        var dataWith = data with { JsonPath = path };
        if (GameRepository.ExistsByJsonPath(path))
            GameRepository.UpdateGame(dataWith);
        else
            GameRepository.InsertGame(dataWith);
        return ValueTask.CompletedTask;
    }

    public static ApplicationJsonData LoadJson(string path)
    {
        return GameRepository.GetByJsonPath(path) ?? default;
    }

    // 廃止: CheckJsonData, CheckAppDataContent
}
```

## 既存API互換の `JsonControl`（詳細）

上記コードブロックを参照。`LoadJson` は元々同期メソッドなのでそのまま。


## 呼び出し元への影響（全件精査）

### 変更不要箇所（シグネチャ維持）

| ファイル:行 | 呼び出し | 理由 |
|---|---|---|
| `GenreManageWindow.xaml.cs:38` | `CreateExeJson(_data.JsonPath, _data)` | シグネチャ維持 |
| `MemoWindow.xaml.cs:57` | 同上 | 同上 |
| `WikiDataManageWindow.xaml.cs:68` | 同上 | 同上 |
| `CreateGameDialog.xaml.cs:143,183` | 同上 | 同上 |
| `Application.xaml.cs:47` | `CreateExeJson(Data.JsonPath, Data)` | 同上 |
| `Web.xaml.cs:46` | 同上 | 同上 |
| `WebGame.xaml.cs:44` | 同上 | 同上 |
| `WebSaver.xaml.cs:46` | 同上 | 同上 |
| `GameWindow.xaml.cs:25` | `LoadJson(jsonPath)` | シグネチャ維持 |
| `GameWindow.xaml.cs:57` | `CreateExeJson(_data.JsonPath, _data)` | 同上 |
| `GameWindow.xaml.cs:307` | `LoadJson(_data.JsonPath)` | 同上 |
| `VolumeWindow.xaml.cs:26` | `CreateExeJson(_data.JsonPath, _data)` | 同上 |
| `GameListPaneControl.xaml.cs:362` | `ReadExeJson($"./Games/{multipleLaunch}.json")` | `JsonControl.ReadExeJson` は `GameRepository.GetByJsonPath` にリダイレクトされるためそのまま動作。パス区切り文字は境界で正規化 |
| `PageControlCreate.cs:435` | `ReadExeJson($"./Games/{multipleLaunch}.json")` | 同上 |
| `GameListPaneControl.xaml.cs:50-55` (`GameControl`) | `Directory.CreateDirectory("./Games")` | `./Games` は exe/html 参照先パス解決用に維持 |
| `FileControl.Main.Directory` | `./Games` 定数 | 変更なし |

### 変更必要箇所

| ファイル:行 | 現状 | 変更内容 |
|---|---|---|
| `App.xaml.cs:40` | `await JsonCheck()` | `await InitializeDatabase()` に置換 |
| `App.xaml.cs:145-154` | `JsonCheck()` 本体 | 削除。`InitializeDatabase()`（マイグレーション＋JSONインポート）に置換 |
| `GameListPaneControl.xaml.cs:73-117` (`LoadGenre`) | `Directory.GetFiles` + `ReadExeJson` | `GameRepository.GetAll()` に置換。`data.Genre` は戻り値から直接取得 |
| `GameListPaneControl.xaml.cs:119-146` (`LoadAllGames`) | 同上 | `GameRepository.GetAll()` |
| `GameListPaneControl.xaml.cs:148-176` (`LoadGamesByGenre`) | 同上 + `data.Genre.Contains` | `GameRepository.GetByGenre(genre)` |
| `GameListPaneControl.xaml.cs:395` | `File.Exists(data.JsonPath)` | `GameRepository.ExistsByJsonPath(data.JsonPath)` |
| `GameListPaneControl.xaml.cs:410` | 同上 | 同上 |
| `GameListPaneControl.xaml.cs:425` | 同上 | 同上 |
| `GameListPaneControl.xaml.cs:440` | 同上 | 同上 |
| `PageControlCreate.cs:68` | `File.Delete(data.JsonPath)` | `GameRepository.DeleteGameByJsonPath(data.JsonPath)` |
| `PageControlCreate.cs:73` | `File.Exists(data.JsonPath)` | `GameRepository.ExistsByJsonPath(data.JsonPath)` |
| `PageControlCreate.cs:75` | `File.Delete(data.JsonPath)` | `GameRepository.DeleteGameByJsonPath(data.JsonPath)` |
| `PageControlCreate.cs:98` | `File.Exists(data.JsonPath)` | `GameRepository.ExistsByJsonPath(data.JsonPath)` |
| `Interface.cs:46` | `Directory.GetFiles("./Games", "*.json")` | `GameRepository.GetAll()` |
| `Interface.cs:54` | `ReadExeJson(jf)` | 46行が `GetAll()` になるのに伴いループ変数 `jf` が `ApplicationJsonData` 型になる。`ReadExeJson` 呼び出し不要で `jf` をそのまま使用 |
| `General.xaml.cs:66-73` (`ExportBtn_OnClick`) | `./Games` のJSONファイルコピー | `games.db` のコピー処理を追加 |
| `General.xaml.cs:129-136` (`AppImportBtn_OnClick`) | `./Games` からのJSONコピー | `games.db` のコピー処理を追加 + 旧JSON形式インポート対応 |
| `JsonControl.cs:14` | `public struct : IEquatable<>` | `public record struct` |
| `JsonControl.cs:38-77` | `Equals` / `GetHashCode` 手書き実装 | 削除（`record struct` が自動生成） |
| `JsonControl.cs:80-107` | `CreateExeJson` / `ReadExeJson` / `LoadJson` の旧実装 | リポジトリ呼び出しに置換 |
| `JsonControl.cs:109-120` | `CheckAppDataContent` | 削除 |
| `JsonControl.cs:122-145` | `CheckJsonData` | 削除 |

### Velopack関連（変更不要）

| ファイル:行 | 処理 | 理由 |
|---|---|---|
| `Program.cs:88-145` (`Temp`) | `../Temp/Games` と `./Games` の入れ替え | DBは `%APPDATA%` なので影響なし。`./Games` は空になるため空ディレクトリ操作になるだけ |
| `App.xaml.cs:93-117` (`temp_file`) | `./Games` を退避 | 同上 |

## 移行ロジック

### 初回起動時の自動移行

`App.xaml.cs` の `Application_Startup` に組み込み。既存 `JsonCheck()` を置換。

```csharp
private static async ValueTask InitializeDatabase()
{
    // 1. ディレクトリ保証
    string appDataDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "YuLauncher");
    if (!Directory.Exists(appDataDir))
        Directory.CreateDirectory(appDataDir);

    // 2. スキーママイグレーション実行
    GameRepository.RunMigrations();

    // 3. 旧形式JSONが残っていればインポート
    await ImportExistingJsonFiles();
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
            var data = JsonSerializer.Deserialize<ApplicationJsonData>(json);
            if (data.Name == null) continue;

            // CheckJsonData と同等の正規化を適用
            // (null フィールドの補完、特に Genre が null の場合は FileExtension からデフォルト判定)
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
            // 失敗したファイルは importedFiles に含めない → backup/ に移動されず残る
        }
    }

    if (importedFiles.Count > 0)
    {
        LoggerController.LogInfo($"Imported {importedFiles.Count} games from JSON to SQLite");

        // 成功したファイルだけをバックアップディレクトリに移動
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

### スキーママイグレーション

```csharp
public static void RunMigrations()
{
    using var conn = CreateConnection();
    using var tx = conn.BeginTransaction();

    // schema_versions テーブル作成（存在しない場合）
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
            // セクション「スキーマ」の全 CREATE TABLE / CREATE INDEX 文
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
```

### 移行ロジックの設計メモ

1. **インポート後のJSONファイル扱い**: `./Games/backup/` に退避。完全削除しないことで、トラブル時の手動復帰を可能にする。ユーザー要件「前データから移行出来る方法」を満たす。
2. **二重インポート防止**: `ExistsByJsonPath` で重複チェック。既にDBに存在する `original_json_path` はスキップ。
3. **移行失敗時の挙動**: 1件失敗しても他件は継続。失敗ログを残す。失敗したファイルは `importedFiles` リストに入らないため `backup/` に移動されず元位置に残り、再試行可能。成功したファイルのみ移動。
4. **`original_json_path` のフォーマット**: `./Games/Syani.json` 形式（相対パス）。既存呼び出し元の `JsonControl.ReadExeJson("./Games/hoge.json")` と整合。
5. **Velopack 更新時のDB扱い**: DBは `%APPDATA%` に置くので、Velopack のパッケージ更新で影響を受けない。
6. **バックアップ/インポート機能（`General.xaml.cs`）**: `games.db` のコピーを追加。旧JSON形式からのインポートは `ImportExistingJsonFiles()` と同じロジックを再利用。

## エラー処理

- **DB接続失敗**: `LoggerController.LogError` で記録、アプリ起動継続（ゲームリストは空表示）
- **マイグレーション失敗**: トランザクションでロールバック、起動継続
- **インポート失敗（1件）**: ログ記録、次ファイルへ継続
- **インポート完全失敗**: JSONファイルを `backup/` に移動せず残す
- **`GameRepository.GetByJsonPath` が null**: `?? default` で `ApplicationJsonData` の既定値を返す（既存 `JsonControl` の挙動と一致）
- **`original_json_path` UNIQUE 制約違反**: `InsertGame` 呼び出し時に既存レコードと衝突する場合、`SqliteException`（SQLite Error 19: UNIQUE constraint failed）が発生。`JsonControl.CreateExeJson` は既に `ExistsByJsonPath` で事前チェックしているため、通常は発生しない。競合が起きた場合は `LoggerController.LogError` で記録し、呼び出し元には例外を伝播（既存の `try-catch` で処理される）
- **`name` の重複**: `name` カラムは UNIQUE 制約なし（同名ゲームが別ファイルとして存在し得る）。`CreateGameDialog` で同名入力時に警告するUI対応は別タスク。本作業では既存挙動（同名許容）を維持

## 依存関係の追加

`YuLauncher.csproj` に追加:

```xml
<PackageReference Include="Microsoft.Data.Sqlite" Version="9.0.*" />
<PackageReference Include="Dapper" Version="2.1.*" />
```

## テスト方針

本変更はデータ層の移行のため、既存のテストスイートが通ることを確認。新規自動テストは追加しない（既存プロジェクトにWPF UI単体テスト無し、設計ドキュメントで不採用決定）。

検証項目:
- [ ] ビルド成功（0 warnings, 0 errors）
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

## 実装順序（実装計画で詳細化）

1. `YuLauncher.csproj` にパッケージ追加
2. `ApplicationJsonData` を `record struct` 化、手書き `Equals`/`GetHashCode` 削除、`public long Id` フィールド新設
3. `GameRepository.cs` 新設（接続管理 + 全CRUD API + `RunMigrations` 実装を同一クラスに集約）
4. `JsonControl.cs` をリポジトリ呼び出しに置換
5. `App.xaml.cs` の `JsonCheck()` を `InitializeDatabase()` に置換
6. `GameListPaneControl.xaml.cs` の `Directory.GetFiles` / `File.Exists` を置換
7. `PageControlCreate.cs` の `File.Exists` / `File.Delete` を置換
8. `Interface.cs` の `Directory.GetFiles` を置換
9. `General.xaml.cs` のバックアップ/インポートに `games.db` 追加
10. `PageControlCreate.LaunchApplication` 内で起動成功後に `GameRepository.RecordPlay(data.Id)` 挿入
11. ビルド検証 + 手動動作確認
