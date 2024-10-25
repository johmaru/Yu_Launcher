using System;
using System.IO;
using System.Reactive.Subjects;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Navigation;
using Wpf.Ui;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;
using YuLauncher.Core.lib;
using YuLauncher.Properties;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace YuLauncher.Core.Window.Pages
{
    /// <summary>
    /// MainPage.xaml の相互作用ロジック
    /// </summary>
    public partial class MainPage : Page
    {
        private ApplicationTheme _theme = new ThemeService().GetTheme();
        private static Subject<int> _settingWindowClose = new Subject<int>();
        public static IObservable<int> SettingWindowClose => _settingWindowClose;
        
        public MainPage()
        {
            InitializeComponent();

            Init();
            
            LoggerController.LogInfo("MainPage Initialized");
        }

        private async void Init()
        {
            await Dispatcher.InvokeAsync(() =>
            {
                MainGrid.Height = MainWindow.WindowHeight;
                Frame.Source = new Uri("GameList.xaml", UriKind.Relative);
            });
        }
        

        private void ExitBtn_OnClick(object sender, RoutedEventArgs e)
        {
           Application.Current.Shutdown();
        }

        private void SettingBtn_OnClick(object sender, RoutedEventArgs e)
        {
            SettingWindow settingWindow = new SettingWindow
            {
                Owner = Application.Current.MainWindow
            };
            
            settingWindow.Closed += (o, args) =>
            {
                _settingWindowClose.OnNext(0);
            };
            
            settingWindow.Show();
        }

        private void GameListBtn_OnClick(object sender, RoutedEventArgs e)
        {
            Frame.Source = new Uri("GameList.xaml", UriKind.Relative);
        }

        private void WebGameListBtn_OnClick(object sender, RoutedEventArgs e)
        {
            Frame.Source = new Uri("WebGameList.xaml", UriKind.Relative);
        }

        private void WebSaverBtn_OnClick(object sender, RoutedEventArgs e)
        {
            Frame.Source = new Uri("WebSaverList.xaml", UriKind.Relative);
        }
        
        private void Main_OnDragEnter(object sender, DragEventArgs e)
        {
            
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[]? files = (string[])e.Data.GetData(DataFormats.FileDrop) ?? throw new InvalidOperationException();
                if (files.Length > 0)
                {
                    string fileExtension = System.IO.Path.GetExtension(files[0]);
                    if (fileExtension == ".exe")
                    {
                        e.Effects = DragDropEffects.Copy;
                    }
                    else
                    {
                        e.Effects = DragDropEffects.None;
                    }
              
                }
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
        }

        private void Main_OnDrop(object sender, DragEventArgs e)
        {
            string[]? files = (string[])e.Data.GetData(DataFormats.FileDrop) ?? throw new InvalidOperationException();
            if (files.Length > 0)
            {
                foreach (var f in files)
                {
                    
                    CreateGameDialog createGameDialog = new CreateGameDialog(f);
                    createGameDialog.Show();
                }
            }
        }
    }
}
