using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Threading.Tasks;
using Microsoft.Web.WebView2.Core;
using Wpf.Ui.Controls;
using YuLauncher.Core.lib;
using YuLauncher.Core.Window;
using YuLauncher.Core.Window.Pages;
using MenuItem = Wpf.Ui.Controls.MenuItem;

namespace YuLauncher.Game.Window;

public partial class GameWindow : FluentWindow
{
    private JsonControl.ApplicationJsonData _data;
    private GameWindow ThisGameWindow { get; }

    public GameWindow(string url, string jsonPath)
    {
        InitializeComponent();

        _data = JsonControl.LoadJson(jsonPath);

        WebView.CoreWebView2InitializationCompleted += WebView_OnCoreWebView2InitializationCompleted;

        try
        {
            if (_data is { FileExtension: "WebGame", Volume: null })
            {
                _data = _data with { Volume = 1.0 };
                _ = PersistAsync();
            }
        }
        catch (Exception e)
        {
            LoggerController.LogError($"{e}");
        }

        ThisGameWindow = this;
        WebView.Source = new Uri(url);

        if (_data.WindowWidth.HasValue && _data.WindowHeight.HasValue)
        {
            Width = _data.WindowWidth.Value;
            Height = _data.WindowHeight.Value;
        }
        else
        {
            var tomlWidth = ManualTomlSettings.GetSettingWindowResolution(FileControl.Main.Settings, "GameResolution", "Width");
            var tomlHeight = ManualTomlSettings.GetSettingWindowResolution(FileControl.Main.Settings, "GameResolution", "Height");
            Width = double.Parse(tomlWidth);
            Height = double.Parse(tomlHeight);
        }

        RebuildWikiDataMenu();
    }

    private async Task PersistAsync()
    {
        try
        {
            await JsonControl.CreateExeJson(_data.JsonPath, _data);
        }
        catch (Exception ex)
        {
            LoggerController.LogError($"{ex}");
        }
    }

    private void RebuildWikiDataMenu()
    {
        WikiDataContentItem.Items.Clear();
        if (_data.WikiData == null) return;

        foreach (var wiki in _data.WikiData)
        {
            var menuItem = new MenuItem { Header = wiki.Key };
            menuItem.Click += (_, _) => OpenWikiUrl(wiki.Value);
            WikiDataContentItem.Items.Add(menuItem);
        }
    }

    private void OpenWikiUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch (Exception)
        {
            System.Windows.MessageBox.Show(
                LocalizeControl.GetLocalize<string>("SimpleUrlError"),
                LocalizeControl.GetLocalize<string>("SimpleBrowser"),
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Warning);
        }
    }

    private async void GameWindow_OnLoaded(object sender, RoutedEventArgs e)
    {
        if (WebView.CoreWebView2 == null)
        {
            await WebView.EnsureCoreWebView2Async();
        }
    }

    private void Resize()
    {
        if (_data.WindowWidth.HasValue && _data.WindowHeight.HasValue)
        {
            Width = _data.WindowWidth.Value;
            Height = _data.WindowHeight.Value;
        }
        else
        {
            var tomlWidth = ManualTomlSettings.GetSettingWindowResolution(FileControl.Main.Settings, "GameResolution", "Width");
            var tomlHeight = ManualTomlSettings.GetSettingWindowResolution(FileControl.Main.Settings, "GameResolution", "Height");
            Width = double.Parse(tomlWidth);
            Height = double.Parse(tomlHeight);
        }
    }

    private void CoreWebView2_ContextMenuRequested(object? sender, CoreWebView2ContextMenuRequestedEventArgs e)
    {
        LoggerController.LogInfo("CoreWebView2_ContextMenuRequested fired");
        e.MenuItems.Clear();

        var controlMenu = WebView.CoreWebView2.Environment.CreateContextMenuItem(
            LocalizeControl.GetLocalize<string>("SimpleControlMenu"),
            null,
            CoreWebView2ContextMenuItemKind.Submenu
        );

        var menuForward = WebView.CoreWebView2.Environment.CreateContextMenuItem(
            LocalizeControl.GetLocalize<string>("SimpleForward"),
            null,
            CoreWebView2ContextMenuItemKind.Command
        );

        var menuBack = WebView.CoreWebView2.Environment.CreateContextMenuItem(
            LocalizeControl.GetLocalize<string>("SimpleBack"),
            null,
            CoreWebView2ContextMenuItemKind.Command
        );

        var menuReload = WebView.CoreWebView2.Environment.CreateContextMenuItem(
            LocalizeControl.GetLocalize<string>("SimpleReload"),
            null,
            CoreWebView2ContextMenuItemKind.Command
        );

        var volumeMenu = WebView.CoreWebView2.Environment.CreateContextMenuItem(
            LocalizeControl.GetLocalize<string>("SimpleVolume"),
            null,
            CoreWebView2ContextMenuItemKind.Command
        );

        menuForward.CustomItemSelected += (_, _) => WebView.GoForward();
        menuBack.CustomItemSelected += (_, _) => WebView.GoBack();
        menuReload.CustomItemSelected += (_, _) => WebView.Reload();

        volumeMenu.CustomItemSelected += (_, _) =>
        {
            var volumeWindow = new VolumeWindow(_data, OnVolumeSaved)
            {
                Owner = this
            };
            volumeWindow.Show();
        };

        controlMenu.Children.Add(menuForward);
        controlMenu.Children.Add(menuBack);
        controlMenu.Children.Add(menuReload);
        controlMenu.Children.Add(volumeMenu);
        e.MenuItems.Add(controlMenu);

        var muteLabel = LocalizeControl.GetLocalize<string>(WebView.CoreWebView2.IsMuted ? "SimpleMuteEnable" : "SimpleMuteDisable");
        var muteMenu = WebView.CoreWebView2.Environment.CreateContextMenuItem(
            muteLabel,
            null,
            CoreWebView2ContextMenuItemKind.Command
        );

        muteMenu.CustomItemSelected += async (_, _) =>
        {
            WebView.CoreWebView2.IsMuted = !WebView.CoreWebView2.IsMuted;
            _data = _data with { IsMute = WebView.CoreWebView2.IsMuted };
            await PersistAsync();
        };

        e.MenuItems.Add(muteMenu);

        var settingMenu = WebView.CoreWebView2.Environment.CreateContextMenuItem(
            LocalizeControl.GetLocalize<string>("SimpleSetting"),
            null,
            CoreWebView2ContextMenuItemKind.Command
        );

        settingMenu.CustomItemSelected += (_, _) =>
        {
            var settingWindow = new SettingWindow
            {
                Owner = this
            };
            settingWindow.Closed += (_, _) => Resize();
            settingWindow.Show();
        };

        e.MenuItems.Add(settingMenu);
    }

    private void OnVolumeSaved(JsonControl.ApplicationJsonData updated)
    {
        _data = updated;
        SetWebViewVolume();
    }

    private void GameWindow_OnClosing(object? sender, CancelEventArgs e)
    {
        WebView.Stop();
        if (WebView.CoreWebView2 != null)
        {
            WebView.CoreWebView2.ContextMenuRequested -= CoreWebView2_ContextMenuRequested;
            WebView.CoreWebView2.NewWindowRequested -= CoreWebView2_NewWindowRequested;
        }
        WebView.CoreWebView2InitializationCompleted -= WebView_OnCoreWebView2InitializationCompleted;
        WebView.Dispose();
    }

    private async void SetWebViewVolume()
    {
        if (_data.Volume == null) return;
        if (WebView.CoreWebView2 == null) return;

        string script = $@"
        document.addEventListener('DOMContentLoaded', function() {{
        var video = document.getElementsByTagName('video')[0];
        if (video) {{
            video.volume = {_data.Volume};
            }}
            }});
                ";
        await WebView.CoreWebView2.ExecuteScriptAsync(script);
    }

    private void WebView_OnCoreWebView2InitializationCompleted(object? sender, CoreWebView2InitializationCompletedEventArgs e)
    {
        if (e.IsSuccess)
        {
            LoggerController.LogInfo("WebView2 initialized, registering handlers");
            WebView.CoreWebView2.ContextMenuRequested += CoreWebView2_ContextMenuRequested;
            WebView.CoreWebView2.NewWindowRequested += CoreWebView2_NewWindowRequested;
            WebView.CoreWebView2.DocumentTitleChanged += (_, _) =>
            {
                Title = WebView.CoreWebView2.DocumentTitle;
            };

            WebView.CoreWebView2.IsMuted = _data.IsMute;

            if (_data.Volume != null)
            {
                SetWebViewVolume();
            }
        }
        else
        {
            LoggerController.LogError("WebView2 Initialization Failed");
            throw new Exception("WebView2 Initialization Failed");
        }
    }

    private void CoreWebView2_NewWindowRequested(object? sender, CoreWebView2NewWindowRequestedEventArgs e)
    {
        e.Handled = true;
        var gameWindow = new GameWindow(e.Uri, _data.JsonPath);
        gameWindow.Loaded += (_, _) => Activate();
        gameWindow.Closing += (_, _) => ThisGameWindow.Activate();
        gameWindow.Show();
    }

    private void SettingItem_OnClick(object sender, RoutedEventArgs e)
    {
        var settingWindow = new SettingWindow
        {
            Owner = this
        };
        settingWindow.Closed += (_, _) => Resize();
        settingWindow.Show();
    }

    private void BackItem_OnClick(object sender, RoutedEventArgs e)
    {
        if (!WebView.CanGoBack) return;
        WebView.GoBack();
    }

    private void ReloadItem_OnClick(object sender, RoutedEventArgs e)
    {
        WebView.Reload();
    }

    private void ForwardItem_OnClick(object sender, RoutedEventArgs e)
    {
        if (!WebView.CanGoForward) return;
        WebView.GoForward();
    }

    private void WikiDataItem_OnClick(object sender, RoutedEventArgs e)
    {
        var wikiWindow = new WikiDataManageWindow(_data)
        {
            Owner = this
        };
        wikiWindow.Closed += (_, _) =>
        {
            _data = JsonControl.LoadJson(_data.JsonPath);
            RebuildWikiDataMenu();
        };
        wikiWindow.Show();
    }
}
