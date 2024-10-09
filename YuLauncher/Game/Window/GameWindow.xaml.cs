
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
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
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Core.Raw;
using Wpf.Ui.Controls;
using YuLauncher.Core.lib;
using YuLauncher.Core.Window;
using YuLauncher.Core.Window.Pages;
using YuLauncher.Properties;

namespace YuLauncher.Game.Window
{
    /// <summary>
    /// GameWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class GameWindow : FluentWindow
    {
        public GameWindow(string url,JsonControl.ApplicationJsonData data)
        {
            InitializeComponent();

           WebView.Source = new Uri(url);

           ManualTomlSettings manualTomlSettings = new ManualTomlSettings();
            var tomlWidth = manualTomlSettings.GetSettingWindowResolution(FileControl.Main.Settings, "GameResolution", "Width");
           var tomlHeight = manualTomlSettings.GetSettingWindowResolution(FileControl.Main.Settings, "GameResolution", "Height");

            this.Width = double.Parse(tomlWidth);
            this.Height = double.Parse(tomlHeight);
        }
        
        private void Resize()
        {
            ManualTomlSettings manualTomlSettings = new ManualTomlSettings();
            var tomlWidth = manualTomlSettings.GetSettingWindowResolution(FileControl.Main.Settings,"GameResolution","Width");
            var tomlHeight = manualTomlSettings.GetSettingWindowResolution(FileControl.Main.Settings,"GameResolution","Height");
            
            this.Width = double.Parse(tomlWidth);
            this.Height = double.Parse(tomlHeight);
        }

        private void CoreWebView2_ContextMenuRequested(object? sender, CoreWebView2ContextMenuRequestedEventArgs e)
        {
           e.MenuItems.Clear();
           
           // ControlMenu Object
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
           // Event
           menuForward.CustomItemSelected += (_,_) => { WebView.GoForward(); };
           
           menuBack.CustomItemSelected += (_,_) => { WebView.GoBack(); };
           
           menuReload.CustomItemSelected += (_,_) => { WebView.Reload(); };
           
           // Add
           controlMenu.Children.Add(menuForward);
           
           controlMenu.Children.Add(menuBack);
           
           controlMenu.Children.Add(menuReload);
           
           //ControlMenu Add
           e.MenuItems.Add(controlMenu);

            //Mute Object

            var label = LocalizeControl.GetLocalize<string>(WebView.CoreWebView2.IsMuted ? "SimpleMuteEnable" : "SimpleMuteDisable");

            var muteMenu = WebView.CoreWebView2.Environment.CreateContextMenuItem(
               label ,
                null,
                CoreWebView2ContextMenuItemKind.Command
            );

            muteMenu.CustomItemSelected += (_, _) =>
            {
                WebView.CoreWebView2.IsMuted = WebView.CoreWebView2.IsMuted switch
                {
                    true => false,
                    _ => true
                };
            };

            e.MenuItems.Add(muteMenu);

            // SettingMenu Object

            var settingMenu = WebView.CoreWebView2.Environment.CreateContextMenuItem(
               LocalizeControl.GetLocalize<string>("SimpleSetting"),
               null,
               CoreWebView2ContextMenuItemKind.Command
           );

           settingMenu.CustomItemSelected += (_,_) =>
           {
                var settingWindow = new SettingWindow();
                var url = WebView.Source.ToString();
                settingWindow.Closed += (_,_) =>
                {
                   Resize();
                };
                settingWindow.Show();
           };
           
           e.MenuItems.Add(settingMenu);

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
            WebView.Stop();
            WebView.CoreWebView2.ContextMenuRequested -= CoreWebView2_ContextMenuRequested;
            WebView.Dispose();
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
            if (e.LeftButton != MouseButtonState.Pressed) return;
            if (this.WindowState != WindowState.Maximized) return;
            var point = Mouse.GetPosition(this);
            this.WindowState = WindowState.Normal;
            this.Left = point.X - this.Width / 2;
            this.Top = point.Y;
            this.DragMove();
        }

        private void MinimizeBtn_OnClick(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void WebView_OnCoreWebView2InitializationCompleted(object? sender, CoreWebView2InitializationCompletedEventArgs e)
        {
            if (e.IsSuccess)
            {
                WebView.CoreWebView2.ContextMenuRequested += CoreWebView2_ContextMenuRequested;
            }
            else
            {
                LoggerController.LogError("WebView2 Initialization Failed");
                throw new Exception("WebView2 Initialization Failed");
            }
        }
    }
}

