using System;
using System.ComponentModel;
using System.IO;
using Wpf.Ui.Controls;
using YuLauncher.Core.lib;

namespace YuLauncher.Core.WebSaverWindow
{
    /// <summary>
    /// GameWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class WebSaverWindow : FluentWindow
    {
        public WebSaverWindow(string name,JsonControl.ApplicationJsonData data)
        {
            InitializeComponent();
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string htmlPath = Path.Combine(baseDirectory, $"html/{name}.html");

            webView.Source = new Uri("file:///" + htmlPath);

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


        private void GameWindow_OnClosing(object? sender, CancelEventArgs e)
        {
            webView.Stop();
            webView.Dispose();
        }

    }
}

