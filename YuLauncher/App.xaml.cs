using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Threading.Tasks;
using System.Text.Json;
using System.Windows;
using NLog;
using Velopack;
using Velopack.Locators;
using Velopack.Sources;
using YuLauncher.Core.lib;
using YuLauncher.Core.Window;
using YuLauncher.Game.Window;
using Application = System.Windows.Application;

namespace YuLauncher
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
       public MainWindow? MainWindowInstance { get; set;}
       public LanguageUpdater? LanguageUpdater { get; set; }
        private async void Application_Startup(object sender, StartupEventArgs e)
        { 
            try
            {
                await FirstLunch();

                await LanguageCheck();

                await Initialize();
                await InitializeDatabase();

                _ = UpdateCheck();
            }
            catch (Exception exception)
            {
                LoggerController.LogError($"{exception}");
            }

            LoggerController.LogInfo("Application Start");
        }
        
        private async ValueTask Initialize()
        {
                  LanguageUpdater = new LanguageUpdater();
                  MainWindowInstance =　await Dispatcher.InvokeAsync(() => new MainWindow());
                 MainWindowInstance.Show();
                 LoggerController.LogInfo("App Initialize Complete");
        }

        private static async Task UpdateCheck()
        {
            try
            {
                var mgr = new UpdateManager(new GithubSource(@"https://github.com/johmaru/Yu_Launcher", null, false),
                    new UpdateOptions
                    {
                        AllowVersionDowngrade = true
                    });

                if (!mgr.IsInstalled)
                {
                    LoggerController.LogError("Application is not installed. Update check cannot proceed.");
                    return;
                }

                var newVersionCheck = await mgr.CheckForUpdatesAsync();
                if (newVersionCheck == null) return;

                var result = MessageBox.Show(LocalizeControl.GetLocalize<string>("NewVersionAb"), "Update", MessageBoxButton.YesNo);
                if (result == MessageBoxResult.Yes)
                {
                    await temp_file();
                    await mgr.DownloadUpdatesAsync(newVersionCheck);
                    mgr.ApplyUpdatesAndRestart(newVersionCheck);
                }
            }
            catch (Exception e)
            {
                LoggerController.LogError($"{e}");
            }
        }

        private static ValueTask temp_file()
        {
            string temp = Path.Combine("..", "Temp");
            string fullTemp = Path.GetFullPath(temp);
            if (!Directory.Exists(fullTemp))
            {
                string relativePath = Path.Combine("..", "Temp", "YuLauncher.exe.WebView2");
                string fullPath = Path.GetFullPath(relativePath);
                FileControl.CopyDirectory("YuLauncher.exe.WebView2", fullPath);

                string gamesPath = Path.Combine("..", "Temp", "Games");
                string fullGamesPath = Path.GetFullPath(gamesPath);
                FileControl.CopyDirectory("Games", fullGamesPath);

                string htmlPath = Path.Combine("..", "Temp", "html");
                string fullHtmlPath = Path.GetFullPath(htmlPath);
                FileControl.CopyDirectory("html", fullHtmlPath);

                string settingsPath = Path.Combine("..", "Temp", "settings.toml");
                string fullSettingsPath = Path.GetFullPath(settingsPath);

                File.Copy("settings.toml", fullSettingsPath, true);
            }
            return ValueTask.CompletedTask;
        }
        
        private static ValueTask FirstLunch()
        {
                bool path = File.Exists("./settings.toml");
                if (!path)
                {
                    TomlControl.CreateToml("./settings.toml");
                }
                bool list = File.Exists("./gameList.toml");
                if (!list)
                {
                    TomlControl.CreateGameListToml("./gameList.toml");
                }
                bool html = Directory.Exists("./html");
                if (!html)
                {
                    Directory.CreateDirectory("./html");
                }

                if (!File.Exists("./Games"))
                {
                    Directory.CreateDirectory("./Games");
                }
                LoggerController.LogInfo("First Lunch Check Complete");
                return ValueTask.CompletedTask;
        }

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

                // セカンドパス: 全ゲームインポート後にMultipleLaunchリンクを再解決
                // （1件目インポート時点で対象ゲームが未登録の場合、リンクが欠落するため）
                var allGames = GameRepository.GetAll();
                int relinked = 0;
                foreach (var game in allGames)
                {
                    if (game.MultipleLaunch is { Length: > 0 })
                    {
                        GameRepository.UpdateGame(game);
                        relinked++;
                    }
                }
                if (relinked > 0)
                    LoggerController.LogInfo($"Re-linked MultipleLaunch for {relinked} games");
            }
        }

        private static ValueTask LanguageCheck()
        {
           var result = TomlControl.GetTomlString("./settings.toml", "Language");
           switch (result)
           {
                case "en":
                    LanguageUpdater.UpdateLanguage("en-US");
                     break;
                case "ja":
                    LanguageUpdater.UpdateLanguage("ja-JP");
                     break;
           }
           LoggerController.LogInfo("Language Check Complete");
           return ValueTask.CompletedTask;
        }

        protected override void OnExit(ExitEventArgs e)
        {
            if (MainWindowInstance != null) MainWindowInstance.Close();
            LoggerController.LogInfo("Application Exit");
            base.OnExit(e);
            Current.Shutdown();
        }
    }
}