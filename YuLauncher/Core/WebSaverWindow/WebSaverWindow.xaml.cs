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
        }


        private void GameWindow_OnClosing(object? sender, CancelEventArgs e)
        {
            webView.Stop();
            webView.Dispose();
        }

    }
}

