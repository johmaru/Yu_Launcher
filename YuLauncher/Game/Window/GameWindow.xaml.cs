
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Web.WebView2.Core.Raw;
using Wpf.Ui.Controls;
using YuLauncher.Core.Window.Pages;
using YuLauncher.Properties;

namespace YuLauncher.Game.Window
{
    /// <summary>
    /// GameWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class GameWindow : FluentWindow
    {
        public GameWindow(string url,string[] args)
        {
            InitializeComponent();
            webView.Source = new Uri(url);
        }

        private void webView_Initialized(object sender, EventArgs e)
        {
          
        }

        private void WikiBTN(object sender, RoutedEventArgs e)
        {
            var wikiwindow = new GameWindowAssistant(this.Title);
            wikiwindow.Show();
        }

        private void ExitBtn_OnClick(object sender, RoutedEventArgs e)
        {
            this.Close();
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

        private void GameWindow_OnSizeChanged(object sender, SizeChangedEventArgs e)
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

        private void GameWindow_OnClosing(object? sender, CancelEventArgs e)
        {
            webView.Stop();
            webView.Dispose();
        }

        private void Gamedock_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
         
        }

        private void Menu_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                DragMove();
            }
        }
    }
}

