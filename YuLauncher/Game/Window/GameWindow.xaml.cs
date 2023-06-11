using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using MahApps.Metro.Controls;
using Microsoft.Web.WebView2.Wpf;
using YuLauncher.Properties;

namespace YuLauncher.Game.Window
{

    public partial class GameWindow : MetroWindow
    {
        public GameWindow(string strData)
        {
            InitializeComponent();


            switch (Settings.Default.FullScreen)
            {
                case true:
                    GameWindow1.WindowStyle = WindowStyle.None;

                    GameWindow1.WindowState = WindowState.Maximized;
                    break;
                case false:
                    GameWindow1.WindowStyle = WindowStyle.SingleBorderWindow;

                    GameWindow1.WindowState = WindowState.Normal;
                    break;
            }
           
            //ゲームの情報をLookupで保管
            

            List<(string GameName, string URL, string Name)> _r18GameList = new List<(string GameName, string URL, string Name)>
            {
                ("あいミスR(WebRender)", "https://pc-play.games.dmm.co.jp/play/imys_r/", "あいりすミスティリア!R"),
                ("ミナシゴノシゴト(WebRender)", "https://pc-play.games.dmm.co.jp/play/minashigo_x/", "ミナシゴノシゴトR"),
                ("あやかしランブル(WebRender)", "https://pc-play.games.dmm.co.jp/play/ayarabux/", "あやかしランブル!X"),
                ("戦国†恋姫オンラインX〜奥宴新史〜 (WebRender)", "https://pc-play.games.dmm.co.jp/play/oenshinshix/", "戦国†恋姫オンラインX〜奥宴新史〜"),
                ("DeepOne 虚無と夢幻のフラグメントR(WebRender)", "https://pc-play.games.dmm.co.jp/play/deeponer/", "DeepOne 虚無と夢幻のフラグメントR"),
                ("救世少女メシアガールXおかわり (WebRender)", "https://pc-play.games.dmm.co.jp/play/meshiya-girlsx/", "救世少女メシアガールXおかわり"),
                ("千年戦争アイギスR(WebRender)", "https://pc-play.games.dmm.co.jp/play/aigis/", "千年戦争アイギスR"),
                ("戦国百花伝R(WebRender)", "https://pc-play.games.dmm.co.jp/play/hyakkadenr/", "戦国百花伝R"),
                ("対魔忍RPGX(WebRender)", "https://pc-play.games.dmm.co.jp/play/taimanin_rpgx/", "対魔忍RPGX"),
                ("神姫PROJECT R(WebRender)", "https://pc-play.games.dmm.co.jp/play/kamipror/", "神姫PROJECT R"),
                ("モンスター娘TD〜ボクは絶海の孤島でモン娘たちに溺愛されて困っています〜X(WebRender)", "https://pc-play.games.dmm.co.jp/play/monmusutdx/", "モンスター娘TD"),
                ("ミストトレインガールズ〜霧の世界の車窓から〜 X(WebRender)", "https://pc-play.games.dmm.co.jp/play/MistTrainGirlsX/", "ミストトレインガールズX"),
                ("オトギフロンティア R(WebRender)", "https://pc-play.games.dmm.co.jp/play/otogi_f_r/", "オトギフロンティアR"),
                ("アイ・アム・マジカミDX(WebRender)", "https://pc-play.games.dmm.co.jp/play/magicami_dx/", "アイ・アム・マジカミDX"),
                ("れじぇくろ！〜レジェンド・クローバー〜 X(WebRender)","https://pc-play.games.dmm.co.jp/play/legeclox/","レジェンド・クローバー〜X")
                ,("FLOWER KNIGHT GIRL X(WebRender)","https://games.dmm.co.jp/detail/flower-x/","花騎士X")
                ,("エンジェリックリンクR(WebRender)","https://pc-play.games.dmm.co.jp/play/angelicr/","エンクリR")
                ,("天啓パラドクスX(WebRender)","https://games.dmm.co.jp/detail/tenkeiprdx_x/","テンパラX")
                ,("アライアンスセージ","https://pc-play.games.dmm.co.jp/play/alliancesagesr/","アライアンスセージ")
                ,("プリンセスエンパイア R","https://pc-play.games.dmm.co.jp/play/princessempirer/","プリンセスエンパイア R")
            };
            //strDataから受け取った値とLookup内のGameNameが一致するURLとName要素をWebViewに送信する

            string url = _r18GameList.FirstOrDefault(x => x.GameName == strData).URL;

            /*
             こっちはかなり沼ったので補足すると、まず _r18GameList内に有るStrDataと等しいGameName要素をWhere拡張子を使って調べる
            .SelectでWhereを使って調べたGameName内の要素であるNameを取り出す もしWhereで調べて一致したものがない場合はNullが保証されている
            .FirstOrDefault();でGameName内にあるName要素が複数ある場合でも最初に該当した要素を取り出すように設定している
            */
            var name = _r18GameList.Where(g => g.GameName == strData)

                .Select(g => g.Name)

                .FirstOrDefault();

            string gameTag = name + "TAG"; ;

            var gameView = new WebView2();

            gameView.Tag = gameTag;

            //WebView本体

           // webView.Tag = gameTag;

         webView.Source = new Uri(url);

            this.Title = name;
        }

        private void webView_Initialized(object sender, EventArgs e)
        {
            if (Settings.Default.FullScreen == true)
            {
                var gamewd = SystemParameters.PrimaryScreenWidth;
                var gameht = SystemParameters.PrimaryScreenHeight;

                webView.Height = gameht;

                webView.Width = gamewd;
            }

            if (Settings.Default.FullScreen == false)
            {
                webView.Height = Settings.Default.Window_H;

                webView.Width = Settings.Default.Window_W;
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

        private void GameWindow1_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Application.Current.MainWindow.Show();
        }
    }
}
