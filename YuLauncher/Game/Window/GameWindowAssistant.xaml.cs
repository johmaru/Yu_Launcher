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

namespace YuLauncher.Game.Window
{
    /// <summary>
    /// GameWindowAssistant.xaml の相互作用ロジック
    /// </summary>
    public partial class GameWindowAssistant : MetroWindow
    {
        public GameWindowAssistant(string strData)
        {
            InitializeComponent();
            if (strData == "あいりすミスティリア!R")
            {
                this.Title = "あいミスWIKI";
                webView.Source = new Uri("https://xn--l8je7d7jnef7m6d8j6d.xn--wiki-4i9hs14f.com/");
            }
            else if (strData == "ミナシゴノシゴトR")
            {
                this.Title = "ミナシゴWIKI";
                webView.Source = new Uri("https://minashigo.wikiru.jp/");
            }
            else if (strData == "あやかしランブル!X")
            {
                this.Title = "あやらぶWIKI";
                webView.Source = new Uri("https://ayarabu.wikiru.jp/");
            }

            else if (strData == "戦国†恋姫オンラインX〜奥宴新史〜")
            {
                this.Title = "戦国恋姫WIKI";
                webView.Source = new Uri("https://pc-play.games.dmm.co.jp/play/oenshinshix/");
            }

            else if (strData == "DeepOne 虚無と夢幻のフラグメントR")
            {
                this.Title = "DeepOneWIKI";
                webView.Source = new Uri("https://tonofura.wikiru.jp/");
            }

            else if (strData == "救世少女メシアガールXおかわり")
            {
                this.Title = "メシアガールWIKI";
                webView.Source = new Uri("https://meshiyagirls.wikiru.jp/");
            }
        }

       
    }
}
