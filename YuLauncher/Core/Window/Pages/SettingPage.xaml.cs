using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Wpf.Ui.Controls;
using YuLauncher.Core.lib;
using YuLauncher.Core.Window.Pages.Settings;
using YuLauncher.Properties;

namespace YuLauncher.Core.Window.Pages
{
    /// <summary>
    /// SettingPage.xaml の相互作用ロジック
    /// </summary>
    public partial class SettingPage : Page
    {
        
        public SettingPage()
        {
            InitializeComponent();
            LoggerController.LogInfo("SettingPage Initialized");
        }

        private void VideoVisualItem_OnClick(object sender, RoutedEventArgs e)
        {
            ContentFrame.Source = new Uri("Settings/VideoGraphics.xaml", UriKind.Relative);
        }

        private void GeneralItem_OnClick(object sender, RoutedEventArgs e)
        {
            ContentFrame.Source = new Uri("Settings/General.xaml", UriKind.Relative);
        }

        private void SettingPage_OnLoaded(object sender, RoutedEventArgs e)
        {
           ContentFrame.Source = new Uri("Settings/General.xaml", UriKind.Relative);
        }
    }
    }