using System;
using System.ComponentModel;
using System.Windows;
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

}
