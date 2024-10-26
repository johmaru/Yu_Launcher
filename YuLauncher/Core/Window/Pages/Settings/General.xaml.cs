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

    private void ImportBtn_OnClick(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog()
        {
            
            Title = "Select new Game Folder",
        };

        if (dialog.ShowDialog() != true) return;
        if (dialog.SafeFolderName != "Games") MessageBox.Show("this Folder is not Games");
        if (Directory.Exists("./Games"))
        {
            Directory.GetFiles("./Games").ToList().ForEach(x =>
            {
                string destFileName = Path.Combine(dialog.FolderName, Path.GetFileName(x));
                File.Copy(x, destFileName, true);
            });
            MessageBox.Show(LocalizeControl.GetLocalize<string>("SimpleCompleted"));
        }
        else
        {
            Directory.CreateDirectory("./Games");
        }
    }

    private void AppImportBtn_OnClick(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog()
        {
            Title = "Select old Game Folder",
        };

        if (dialog.ShowDialog() != true) return;
        if (dialog.SafeFolderName != "Games") MessageBox.Show("this Folder is not Games");
        if (Directory.Exists("./Games"))
        {
            Directory.GetFiles(dialog.FolderName).ToList().ForEach(x =>
            {
                string destFileName = Path.Combine("./Games", Path.GetFileName(x));
                File.Copy(x, destFileName, true);
            });
            MessageBox.Show(LocalizeControl.GetLocalize<string>("SimpleCompleted"));
        }
        else
        {
            Directory.CreateDirectory("./Games");
        }
    }
}