using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using Wpf.Ui.Controls;
using YuLauncher.Core.WebSaverWindow;

namespace YuLauncher.Core.WebSaverWindow
{
    /// <summary>
    /// GameWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class WebSaverWindow : FluentWindow
    {
        public WebSaverWindow(string name,string[] args)
        {
            InitializeComponent();
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string htmlPath = Path.Combine(baseDirectory, $"html/{name}.html");
            Console.WriteLine(htmlPath);
            webView.Source = new Uri("file:///" + htmlPath);
        }

        private void webView_Initialized(object sender, EventArgs e)
        {
          
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

        private void UIElement_OnMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (this.WindowState == WindowState.Maximized)
                {
                    var point = Mouse.GetPosition(this);
                    this.WindowState = WindowState.Normal;
                    this.Left = point.X - this.Width / 2;
                    this.Top = point.Y;
                    this.DragMove();
                }
            }
        }
    }
}

