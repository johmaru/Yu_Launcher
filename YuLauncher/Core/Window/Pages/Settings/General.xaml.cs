using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using YuLauncher.Core.lib;

namespace YuLauncher.Core.Window.Pages.Settings;

public partial class General : Page
{
    public General()
    {
        InitializeComponent();
    }

    private void General_OnLoaded(object sender, RoutedEventArgs e)
    {
       ThemePagePatcher.PatchTheme(this);
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
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
            throw;
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
            Console.WriteLine(exception);
            throw;
        }
    }
}