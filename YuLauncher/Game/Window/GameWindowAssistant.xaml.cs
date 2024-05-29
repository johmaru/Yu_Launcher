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

namespace YuLauncher.Game.Window
{
    /// <summary>
    /// GameWindowAssistant.xaml の相互作用ロジック
    /// </summary>
    public partial class GameWindowAssistant : System.Windows.Window
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

            else if (strData == "千年戦争アイギスR")
            {
                this.Title = "アイギスWIKI";
                webView.Source = new Uri("https://wikiwiki.jp/aigiszuki/");
            }

            else if (strData == "戦国百花伝R")
            {
                this.Title = "戦国百花伝R WIKI";
                webView.Source = new Uri("https://sengoku-hyakka.wikiru.jp/");
            }

            else if (strData == "対魔忍RPGX")
            {
                this.Title = "対魔忍RPGX WIKI";
                webView.Source = new Uri("https://taimanin-rpg.wikiru.jp/");
            }

            else if (strData == "神姫PROJECT R")
            {
                this.Title = "神姫PROJECT R WIKI";
                webView.Source = new Uri("https://xn--hckqz0e9cygq471ahu9b.xn--wiki-4i9hs14f.com/");
            }

            else if (strData == "モンスター娘TD")
            {
                this.Title = "もんむすTD WIKI";
                webView.Source = new Uri("https://monmusu-td.wikiru.jp/");
            }

            else if (strData == "ミストトレインガールズX")
            {
                this.Title = "ミストトレインガールズ WIKI";
                webView.Source = new Uri("https://misttraingirls.wikiru.jp/");
            }

            else if (strData == "オトギフロンティア R")
            {
                this.Title = "オトギフロンティア WIKI";
                webView.Source = new Uri("https://otogi.wikiru.jp/");
            }

            else if (strData == "アイ・アム・マジカミDX")
            {
                this.Title = "マジカミ WIKI";
                webView.Source = new Uri("https://magicami.wikiru.jp/");
            }

            else if (strData == "レジェンド・クローバー〜 X")
            {
                this.Title = "レジェクロ WIKI";
                webView.Source = new Uri("https://legeclo.wikiru.jp/");
            }

            else if (strData == "花騎士X")
            {
                this.Title = "花騎士 WIKI";
                webView.Source = new Uri("https://xn--eckq7fg8cygsa1a1je.xn--wiki-4i9hs14f.com/");
            }

            else if (strData == "エンクリR")
            {
                this.Title = "エンクリ WIKI";
                webView.Source = new Uri("https://angelic.wikiru.jp/");
            }

            else if (strData == "天啓パラドクスX")
            {
                this.Title = "テンパラ WIKI";
                webView.Source = new Uri("https://tenkei-paradox.wikiru.jp/");
            }

            else if (strData == "アライアンスセージ")
            {
                this.Title = "アライアンスセージ WIKI";
                webView.Source = new Uri("https://alliancesages.wikiru.jp/");
            }

            else if (strData == "プリンセスエンパイア R")
            {
                MessageBox.Show("現在このゲームのWIKIは在りません", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }

       
    }
}
