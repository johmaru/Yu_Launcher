using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using Wpf.Ui.Controls;
using YuLauncher.Core.lib;

namespace YuLauncher.WebSite;

public partial class WebViewWindow : FluentWindow
{
    public WebViewWindow(string url,JsonControl.ApplicationJsonData data)
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
        WindowState = WindowState.Maximized;
        WindowStateIcon.Glyph = "\uE73F";
    }

    private void WindowStateBtn_OnUnchecked(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Normal;
        WindowStateIcon.Glyph = "\uE740";
    }

    private void ExitBtn_OnClick(object sender, RoutedEventArgs e)
    {
       Close();
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
       string height = TomlControl.GetTomlString(FileControl.Main.Settings, "WebViewResolution", "Height");
       string width = TomlControl.GetTomlString(FileControl.Main.Settings, "WebViewResolution", "Width");

       Width = Convert.ToDouble(width);
       Height = Convert.ToDouble(height);
    }

    private void UIElement_OnMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed) return;
        if (WindowState != WindowState.Maximized) return;
        var point = Mouse.GetPosition(this);
        WindowState = WindowState.Normal;
        Left = point.X - Width / 2;
        Top = point.Y;
        DragMove();
    }

    private void MinimizeBtn_OnClick(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }
}