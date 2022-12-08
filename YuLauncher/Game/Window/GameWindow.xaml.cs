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
using Microsoft.Web.WebView2.Core.Raw;
using YuLauncher.Core.Window.Pages;
using YuLauncher.Properties;

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
            if (strData == "あいミスR(WebRender)")
            {
                webView.Source = new Uri("https://games.dmm.com/detail/imys/");
                this.Title = "あいりすミスティリア!R";
            }
            else if (strData == "ミナシゴノシゴト(WebRender)")
            {
                webView.Source = new Uri("https://pc-play.games.dmm.co.jp/play/minashigo_x/");
                this.Title = "ミナシゴノシゴトR";
            }
            else if (strData == "あやかしランブル(WebRender)")
            {
                webView.Source = new Uri("https://pc-play.games.dmm.co.jp/play/ayarabux/");
                this.Title = "あやかしランブル!X";
            }

            else if (strData == "戦国†恋姫オンラインX〜奥宴新史〜 (WebRender)")
            {
                webView.Source = new Uri("https://pc-play.games.dmm.co.jp/play/oenshinshix/");
                this.Title = "戦国†恋姫オンラインX〜奥宴新史〜";
            }

            else if (strData == "DeepOne 虚無と夢幻のフラグメントR(WebRender)")
            {
                webView.Source = new Uri("https://pc-play.games.dmm.co.jp/play/deeponer/");
                this.Title = "DeepOne 虚無と夢幻のフラグメントR";
            }

            else if (strData == "救世少女メシアガールXおかわり (WebRender)")
            {
                webView.Source = new Uri("https://pc-play.games.dmm.co.jp/play/meshiya-girlsx/");
                this.Title = "救世少女メシアガールXおかわり";
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

        private void webView_Initialized(object sender, EventArgs e)
        {
            if (Settings.Default.FullScreen == true)
            {
                var gamewd = SystemParameters.PrimaryScreenWidth;
                var gameht = SystemParameters.PrimaryScreenHeight;

                webView.Height = gameht;

                webView.Width = gamewd;

                gamedock.Width = gamewd;

                gamedock.Height = gameht;
            }

            if (Settings.Default.FullScreen == false)
            {
                webView.Height = Settings.Default.Window_H;

                webView.Width = Settings.Default.Window_W;

                gamedock.Height = Settings.Default.Window_H;

                gamedock.Width = Settings.Default.Window_W;
            }

            else
            {
              
            }
        }

        private void WikiBTN(object sender, RoutedEventArgs e)
        {
            var wikiwindow = new GameWindowAssistant(this.Title);
            wikiwindow.Show();
        }

        private void GameWindow1_Loaded(object sender, RoutedEventArgs e)
        {
           
        }
    }
}
