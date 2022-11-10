using System;
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
using System.Windows.Shapes;
using MahApps.Metro.Controls;
using YuLauncher.Core.Window.Pages;

namespace YuLauncher.Game.Window
{
    /// <summary>
    /// GameWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class GameWindow : MetroWindow
    {
        public GameWindow(string strData)
        {
            InitializeComponent();
            if (strData == "August_myth")
            {
                webView.Source = new Uri("https://games.dmm.com/detail/imys/");
            }
            else
            {
                return;
            }
            if (Properties.Settings.Default.FullScreen == true)
            {
                this.WindowStyle = WindowStyle.None;
                this.WindowState = WindowState.Maximized;
            }
            if(Properties.Settings.Default.FullScreen == false)
            {
                this.WindowStyle = WindowStyle.SingleBorderWindow;
                this.WindowState = WindowState.Normal;
            }
            else
            {
                return;
            }
        }
    }
}
