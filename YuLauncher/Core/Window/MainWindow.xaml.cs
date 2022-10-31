﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MahApps.Metro.Controls;
using YuLauncher.Core.Pages;
using YuLauncher.Properties;

namespace YuLauncher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            this.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            if (Settings.Default.FullScreen == true)
            {
                this.WindowStyle = WindowStyle.None;
                this.WindowState = WindowState.Maximized;
                this.ResizeMode = ResizeMode.NoResize;
            }

            else
            {
                this.WindowStyle = WindowStyle.SingleBorderWindow;
                this.WindowState = WindowState.Normal;
                this.ResizeMode = ResizeMode.CanResize;
            }
            
        }
        //RecoverWindowSizeはまだ使うか未定.というのもWindow_WとWindow_Hはゲームスクリーンの解像度の値にする予定
        //MainWindow用のSettingの値を作成する可能性が微レ存
        void RecoverWindowSize()
        {
            var settings = Settings.Default;
            if (settings.Window_W > 0 &&
                settings.Window_W <= SystemParameters.WorkArea.Width)
            { Width = settings.Window_W; }

            if (settings.Window_H > 0 &&
                settings.Window_H <= SystemParameters.WorkArea.Height)
            { Height = settings.Window_H; }
        }
        private void MetroWindow_DpiChanged(object sender, DpiChangedEventArgs e)
        {
            this.SizeToContent = SizeToContent.WidthAndHeight;
        }

        private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {

        }
    }
}