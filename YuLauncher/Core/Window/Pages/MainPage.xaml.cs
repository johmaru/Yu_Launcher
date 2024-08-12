using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Controls;
using Wpf.Ui.Controls;
using YuLauncher.Core.lib;
using YuLauncher.Properties;
using JsonSerializer = System.Text.Json.JsonSerializer;
using MessageBox = System.Windows.MessageBox;

namespace YuLauncher.Core.Window.Pages
{
    /// <summary>
    /// MainPage.xaml の相互作用ロジック
    /// </summary>
    public partial class MainPage : Page
    {
        public MainPage()
        {
            InitializeComponent();

            InitializeControl();
            
            LoggerController.LogInfo("MainPage Initialized");
        }

        private void InitializeControl()
        {
            
        }
        

        private void ExitBtn_OnClick(object sender, RoutedEventArgs e)
        {
           Application.Current.Shutdown();
        }

        private void SettingBtn_OnClick(object sender, RoutedEventArgs e)
        {
            SettingWindow settingWindow = new SettingWindow();
            settingWindow.Owner = Application.Current.MainWindow;
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

        private void FavGameList_OnClick(object sender, RoutedEventArgs e)
        {
          MessageBox.Show("Coming Soon");
        }

        private void LoginHistoryBtn_OnClick(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Coming Soon");
        }
    }
}
