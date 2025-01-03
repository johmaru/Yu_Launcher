﻿
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
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
using Button = Wpf.Ui.Controls.Button;
using MenuItem = Wpf.Ui.Controls.MenuItem;
using MessageBox = System.Windows.MessageBox;

namespace YuLauncher.Game.Window
{
    /// <summary>
    /// GameWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class GameWindow : FluentWindow
    {
        private JsonControl.ApplicationJsonData _data;
        
        private GameWindow ThisGameWindow { get; }
        public GameWindow(string url,string jsonPath)
        {
            InitializeComponent();


            _data = JsonControl.LoadJson(jsonPath);
        
            
            WebView.CoreWebView2InitializationCompleted += WebView_OnCoreWebView2InitializationCompleted;   

            try
            {
                if (_data is { FileExtension: "WebGame", Volume: null })
                {
                    JsonControl.ApplicationJsonData newData = _data with { Volume = 1.0 };
                    _ = JsonControl.CreateExeJson(_data.JsonPath, newData);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            ThisGameWindow = this;

            WebView.Source = new Uri(url);

            var tomlWidth = ManualTomlSettings.GetSettingWindowResolution(FileControl.Main.Settings, "GameResolution", "Width");
           var tomlHeight = ManualTomlSettings.GetSettingWindowResolution(FileControl.Main.Settings, "GameResolution", "Height");

            Width = double.Parse(tomlWidth);
            Height = double.Parse(tomlHeight);

            foreach (var wikidata in _data.WikiData)
            {
                var menuItem = new MenuItem()
                {
                    Header = wikidata.Key
                };
                menuItem.Click += (_, _) =>
                {
                    try
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = wikidata.Value,
                            UseShellExecute = true
                        });
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show(LocalizeControl.GetLocalize<string>("SimpleUrlError"));
                    }
                };
                WikiDataContentItem.Items.Add(menuItem);
            }
        }
        
        private void Resize()
        {
            var tomlWidth = ManualTomlSettings.GetSettingWindowResolution(FileControl.Main.Settings,"GameResolution","Width");
            var tomlHeight = ManualTomlSettings.GetSettingWindowResolution(FileControl.Main.Settings,"GameResolution","Height");
            
            Width = double.Parse(tomlWidth);
            Height = double.Parse(tomlHeight);
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
           
           var volumeMenu = WebView.CoreWebView2.Environment.CreateContextMenuItem(
               LocalizeControl.GetLocalize<string>("SimpleVolume"),
               null,
               CoreWebView2ContextMenuItemKind.Command
           );
           
           // Event
           menuForward.CustomItemSelected += (_,_) => { WebView.GoForward(); };
           
           menuBack.CustomItemSelected += (_,_) => { WebView.GoBack(); };
           
           menuReload.CustomItemSelected += (_,_) => { WebView.Reload(); };
           
              volumeMenu.CustomItemSelected += (_,_) =>
              {
                  var stackPanel = new StackPanel()
                  {
                      Orientation = Orientation.Vertical,
                      HorizontalAlignment = HorizontalAlignment.Center,
                      VerticalAlignment = VerticalAlignment.Center
                  };
                  var button = new Button()
                  {
                      Content = LocalizeControl.GetLocalize<string>("SimpleSave"),
                      Margin = new Thickness(10, 10, 10, 10),
                      Width = 80,
                      Height = 30
                  };
                  
                  
                  var slider = new Slider()
                  {
                      Minimum = 0,
                      Maximum = 1,
                      Value = _data.Volume ?? 1,
                      IsSnapToTickEnabled = true,
                      TickFrequency = 0.1,
                      TickPlacement = TickPlacement.BottomRight,
                      IsDirectionReversed = true,
                      VerticalAlignment = VerticalAlignment.Center,
                      HorizontalAlignment = HorizontalAlignment.Center,
                      Margin = new Thickness(10, 10, 10, 10),
                      Width = 80,
                      Height = 100,
                      Orientation = Orientation.Vertical
                  };
                  
                  slider.ValueChanged += (_,_) =>
                  {
                      
                  };
                  
                  var volumeWindow = new FluentWindow()
                  {
                      Height = 200,
                      Width = 100,
                      ResizeMode = ResizeMode.NoResize,
                      WindowStartupLocation = WindowStartupLocation.CenterScreen,
                      Title = LocalizeControl.GetLocalize<string>("SimpleVolume"),
                      Content = stackPanel
                  };
                    volumeWindow.Show();
                    
                    //event 
                    button.Click += (_,_) =>
                    {
                        _data = _data with { Volume =  slider.Value = Math.Round(slider.Value, 1) };
                        _ = JsonControl.CreateExeJson(_data.JsonPath, _data);
                        SetWebViewVolume();
                        volumeWindow.Close();
                    };
                  
                    stackPanel.Children.Add(slider);
                    stackPanel.Children.Add(button);
              };
           
           // Add
           controlMenu.Children.Add(menuForward);
           
           controlMenu.Children.Add(menuBack);
           
           controlMenu.Children.Add(menuReload);
           
              controlMenu.Children.Add(volumeMenu);
           
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
                var newData = _data with { IsMute = WebView.CoreWebView2.IsMuted };
                _ = JsonControl.CreateExeJson(_data.JsonPath, newData);
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
            Close();
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
            WebView.CoreWebView2.NewWindowRequested -= CoreWebView2_NewWindowRequested;
            WebView.CoreWebView2InitializationCompleted -= WebView_OnCoreWebView2InitializationCompleted;
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
        
        private async void SetWebViewVolume()
        {
            if (_data.Volume == null) return;

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
            GameWindow gameWindow = new GameWindow(e.Uri, _data.JsonPath)
            {
                
            };
            gameWindow.Loaded += (_, _) =>
            {
                Activate();
            };
            gameWindow.Closing += (_, _) =>
            {
                ThisGameWindow.Activate();
            };
            gameWindow.Show();
        }

        private void SettingItem_OnClick(object sender, RoutedEventArgs e)
        {
            SettingWindow settingWindow = new SettingWindow
            {
                Owner = this
            };
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
            WikiDataManageWindow wikiDataManageWindow = new WikiDataManageWindow(_data)
            {
                Owner = this
            };
            wikiDataManageWindow.Closed += (o, args) =>
            {
               var  data = JsonControl.LoadJson(_data.JsonPath);
                _data = data;
                
                WikiDataContentItem.Items.Clear();
                
                foreach (var wikidata in _data.WikiData)
                {
                    var menuItem = new MenuItem()
                    {
                        Header = wikidata.Key
                    };
                    menuItem.Click += (_, _) =>
                    {
                        try
                        {
                            Process.Start(new ProcessStartInfo
                            {
                                FileName = wikidata.Value,
                                UseShellExecute = true
                            });
                        }
                        catch (Exception exception)
                        {
                          MessageBox.Show(LocalizeControl.GetLocalize<string>("SimpleUrlError"));
                        }
                    };
                    WikiDataContentItem.Items.Add(menuItem);
                }
            };
            wikiDataManageWindow.Show();
        }

        private void UIElement_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if(e.ChangedButton == MouseButton.Left)
                DragMove();
        }

        private void menu_OnMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }
    }
}

