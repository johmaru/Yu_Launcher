using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Velopack;
using Velopack.Sources;
using YuLauncher.Core.lib;

namespace YuLauncher.Core.Window.Pages.Settings;

public class Data()
{
    public string NowVersion { get; set; } = string.Empty;
}

public partial class General : Page
{
    public General()
    {
        InitializeComponent();
        var fallbackVersion = typeof(General).Assembly.GetName().Version?.ToString() ?? "dev";
        var currentVersion = fallbackVersion;

        try
        {
            var mgr = new UpdateManager(new GithubSource(@"https://github.com/johmaru/Yu_Launcher", null, false),
                new UpdateOptions
                {
                    AllowVersionDowngrade = true
                });

            if (mgr.IsInstalled && mgr.CurrentVersion != null)
            {
                currentVersion = mgr.CurrentVersion.ToString();
            }
        }
        catch (Exception exception)
        {
            LoggerController.LogWarn("Velopack is not available in this session: " + exception.Message);
        }
        DataContext = new Data()
        {
            NowVersion = $"{LocalizeControl.GetLocalize<string>("NowVersion")} : {currentVersion}"
        };
    }

    private void General_OnLoaded(object sender, RoutedEventArgs e)
    {
       LoadDividerColor();
    }

    private void LoadDividerColor()
    {
        string color;
        if (!TomlControl.TryGetString("./settings.toml", "DividerColor", out var raw) || string.IsNullOrWhiteSpace(raw))
            color = "#8B5CF6";
        else
            color = raw;
        DividerColorBox.Text = color;
        UpdatePreview(color);
    }

    private void UpdatePreview(string color)
    {
        try
        {
            var brush = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(color));
            DividerColorPreview.Background = brush;
        }
        catch
        {
            // 無効な色文字列はプレビューを更新しない
        }
    }

    private void DividerColorApplyBtn_OnClick(object sender, RoutedEventArgs e)
    {
        var color = DividerColorBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(color))
            color = "#8B5CF6";

        // 色の有効性を検証
        try
        {
            _ = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(color);
        }
        catch
        {
            return;
        }

        // settings.toml に保存
        TomlControl.EditToml("./settings.toml", "DividerColor", color);

        // アプリ全体の DividerBrush を更新
        var brush = new System.Windows.Media.SolidColorBrush(
            (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(color));
        System.Windows.Application.Current.Resources["DividerBrush"] = brush;

        UpdatePreview(color);
    }

    private void ExportBtn_OnClick(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog()
        {
            
            Title = "Select new YuLauncher Folder",
        };

        if (dialog.ShowDialog() != true) return;
        
        try
        {
            if (Directory.Exists("./Games"))
            {
                Directory.GetFiles("./Games").ToList().ForEach(x =>
                {
                    string destFileName = Path.Combine(dialog.FolderName + "/Games", Path.GetFileName(x));
                    File.Copy(x, destFileName, true);
                });
            }
            else
            {
                Directory.CreateDirectory("./Games");
            }
            
            if (Directory.Exists("./html"))
            {
                Directory.GetFiles("./html").ToList().ForEach(x =>
                {
                    string destFileName = Path.Combine(dialog.FolderName + "/html", Path.GetFileName(x));
                    File.Copy(x, destFileName, true);
                });

            }
            else
            {
                Directory.CreateDirectory("./html");
            }
            
            if (File.Exists("settings.toml"))
            {
                string destFileName = Path.Combine(dialog.FolderName, "settings.toml");
                File.Copy("settings.toml", destFileName, true);
            }
            else
            {
                MessageBox.Show("settings.toml not found");
            }

            if (Directory.Exists("YuLauncher.exe.WebView2"))
            {
                FileControl.CopyDirectory("YuLauncher.exe.WebView2", Path.Combine(dialog.FolderName, "YuLauncher.exe.WebView2"));
            }
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
        }
        catch (Exception exception)
        {
            LoggerController.LogError($"{exception}");
            
        }
        
        MessageBox.Show(LocalizeControl.GetLocalize<string>("SimpleCompleted"));
        
    }

    private void AppImportBtn_OnClick(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog()
        {
            Title = "Select old Game Folder",
        };

        if (dialog.ShowDialog() != true) return;

        try
        {
            if (Directory.Exists(dialog.FolderName + "/Games"))
            {
                Directory.GetFiles(dialog.FolderName + "/Games").ToList().ForEach(x =>
                {
                    string destFileName = Path.Combine("./Games", Path.GetFileName(x));
                    File.Copy(x, destFileName, true);
                });
            }
            else
            {
                MessageBox.Show("Games Folder not found");
            }
            
            if (Directory.Exists(dialog.FolderName + "/html"))
            {
                Directory.GetFiles(dialog.FolderName + "/html").ToList().ForEach(x =>
                {
                    string destFileName = Path.Combine("./html", Path.GetFileName(x));
                    File.Copy(x, destFileName, true);
                });

            }
            else
            {
                MessageBox.Show("html Folder not found");
            }
            
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

            if (File.Exists(dialog.FolderName + "/settings.toml"))
            {
                string destFileName = Path.Combine("./settings.toml");
                File.Copy(dialog.FolderName + "/settings.toml", destFileName, true);
            }
            else
            {
                MessageBox.Show("settings.toml not found");
            }
            
            if (Directory.Exists(dialog.FolderName + "/YuLauncher.exe.WebView2"))
            {
                FileControl.CopyDirectory(dialog.FolderName + "/YuLauncher.exe.WebView2", "YuLauncher.exe.WebView2");
            }
        }
        catch (Exception exception)
        {
            LoggerController.LogError($"{exception}");
            
        }
    }
}
