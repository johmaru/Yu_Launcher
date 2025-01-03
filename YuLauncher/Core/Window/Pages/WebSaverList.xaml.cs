﻿using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using YuLauncher.Core.lib;
using Button = Wpf.Ui.Controls.Button;

namespace YuLauncher.Core.Window.Pages;

public partial class WebSaverList : Page
{
    private static string[]? _files;
    private readonly GameButton _gameButton = new();
    public WebSaverList()
    {
        InitializeComponent();

        PageControlCreate.DeleteFileMenuClicked.Subscribe(_ => PageControlCreateOnOnDeleteFileMenuClicked(this, EventArgs.Empty));
        CreateGameDialog.CloseObservable.Subscribe(_ => PropertyDialogOnOnGameListWebSaverPanelUpdate(this, EventArgs.Empty));
        PropertyDialog.AllGameListPanelUpdate.Subscribe( _ => PropertyDialogOnOnGameListWebSaverPanelUpdate(this, EventArgs.Empty));
        Initialize();
    }

    private async void PageControlCreateOnOnDeleteFileMenuClicked(object? sender, EventArgs e)
    {
        _files = null;
        await Task.Run(() => _files = Directory.GetFiles(FileControl.Main.Directory));
        SaverListLoad();
    }

    private async void PropertyDialogOnOnGameListWebSaverPanelUpdate(object? sender, EventArgs e)
    {
        _files = null;
        await Task.Run(() => _files = Directory.GetFiles(FileControl.Main.Directory));
        SaverListLoad();
    }

    private async void SaverListLoad()
    {
        Panel.Children.Clear();
        if (_files == null) return;
        foreach (var file in _files)
        {
            if (Path.GetExtension(file) != ".json") continue;
            JsonControl.ApplicationJsonData data = await JsonControl.ReadExeJson(file);
            try
            {
                if (data.FileExtension == "WebSaver")
                {
                    Panel.Children.Add(_gameButton.GameButtonShow(data.Name, data));
                }
            }
            catch (Exception e)
            {
                LoggerController.LogError(e.Message);
            }
        }
    }

    private async void Initialize()
    {
        Panel.Children.Clear();
        
        await Task.Run(() => _files = Directory.GetFiles(FileControl.Main.Directory));
        SaverListLoad();
    }

    private void AddButton_OnClick(object sender, RoutedEventArgs e)
    {
        CreateGameDialog createGameDialog = new CreateGameDialog();
        createGameDialog.Show();
    }

    private void WebSaverList_OnPreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        DependencyObject? source = e.OriginalSource as DependencyObject;
        while (source != null && !(source is Button))
        {
            source = VisualTreeHelper.GetParent(source);
        }

        if (source is Button button)
        {
            ContextMenu = button.ContextMenu;
        }
        else
        {
            ContextMenu = PageControlCreate.GameListShowContextMenu(false,new JsonControl.ApplicationJsonData());
        }
    }
}