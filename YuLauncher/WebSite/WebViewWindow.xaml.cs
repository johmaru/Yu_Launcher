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

        if (data.WindowWidth.HasValue && data.WindowHeight.HasValue)
        {
            Width = data.WindowWidth.Value;
            Height = data.WindowHeight.Value;
        }
        else
        {
            string width = TomlControl.GetTomlString(FileControl.Main.Settings, "WebViewResolution", "Width");
            string height = TomlControl.GetTomlString(FileControl.Main.Settings, "WebViewResolution", "Height");
            Width = Convert.ToDouble(width);
            Height = Convert.ToDouble(height);
        }
    }


    private void WebViewWindow_OnClosing(object? sender, CancelEventArgs e)
    {
        webView.Stop();
        webView.Dispose();
    }

}
