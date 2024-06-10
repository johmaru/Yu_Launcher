using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using Wpf.Ui.Controls;
using YuLauncher.Core.lib;

namespace YuLauncher.WebSite;

public partial class WebViewWindow : FluentWindow
{
    public WebViewWindow(string url,string[] args)
    {
        InitializeComponent();
        webView.Source = new Uri(url);
    }

    private void Menu_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
        {
            DragMove();
        }
    }

    private void WindowStateBtn_OnChecked(object sender, RoutedEventArgs e)
    {
        this.WindowState = WindowState.Maximized;
        WindowStateIcon.Glyph = "\uE73F";
    }

    private void WindowStateBtn_OnUnchecked(object sender, RoutedEventArgs e)
    {
        this.WindowState = WindowState.Normal;
        WindowStateIcon.Glyph = "\uE740";
    }

    private void ExitBtn_OnClick(object sender, RoutedEventArgs e)
    {
       this.Close();
    }

    private void WebViewWindow_OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        switch (WindowState)
        {
            case WindowState.Maximized:
                WindowStateIcon.Glyph = "\uE73F";
                WindowStateBtn.IsChecked = true;
                break;
            case WindowState.Normal:
                WindowStateIcon.Glyph = "\uE740";
                WindowStateBtn.IsChecked = false;
                break;
            case WindowState.Minimized:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void WebViewWindow_OnClosing(object? sender, CancelEventArgs e)
    {
        webView.Stop();
        webView.Dispose();
    }

    private void WebViewWindow_OnLoaded(object sender, RoutedEventArgs e)
    {
       string height = TomlControl.GetTomlStringList(FileControl.Main.Settings, "WebViewResolution", "Height");
       string width = TomlControl.GetTomlStringList(FileControl.Main.Settings, "WebViewResolution", "Width");

       Width = Convert.ToDouble(width);
       Height = Convert.ToDouble(height);
    }
}