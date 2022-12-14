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
using System.Windows.Navigation;
using System.Windows.Shapes;
using MahApps.Metro.Controls;
using YuLauncher.Game;
using YuLauncher.Game.Window;
using YuLauncher.Properties;

namespace YuLauncher.Core.Window.Pages
{
    /// <summary>
    /// GameListPage.xaml の相互作用ロジック
    /// </summary>
    public partial class GameListPage : Page
    {
       
        public GameListPage()
        {
            InitializeComponent();
        }

       
        private void August_myth_Click(object sender, RoutedEventArgs e)
        {
          var gamewindow = new GameWindow(GameListBTN1.Content.ToString());
           gamewindow.Show();
        }

        private void backbtn_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }

        private void Minasigo_OnClick(object sender, RoutedEventArgs e)
        {
            var gamewindow = new GameWindow(GameListBTN2.Content.ToString());
            gamewindow.Show();
        }

        private void Ayarabu_OnClick(object sender, RoutedEventArgs e)
        {
            var gamewindow = new GameWindow(GameListBTN3.Content.ToString());
            gamewindow.Show();
        }

        private void Sengoku_KoiHime_OnClick(object sender, RoutedEventArgs e)
        {
            var gamewindow = new GameWindow(GameListBTN4.Content.ToString());
            gamewindow.Show();
        }

        private void DeepOne_OnClick(object sender, RoutedEventArgs e)
        {
            var gamewindow = new GameWindow(GameListBTN5.Content.ToString());
            gamewindow.Show();
        }

        private void MesiGirl_OnClick(object sender, RoutedEventArgs e)
        {
            var gamewindow = new GameWindow(GameListBTN6.Content.ToString());
            gamewindow.Show();
        }

        //ページが戻ったときの処理
        private void BackPage_OnClick(object sender, RoutedEventArgs e)
        {
            if (GameListBTN1.Content.ToString() == "あいミスR(WebRender)")
            {
                MessageBox.Show("これ以上後ろのページは有りません");
            }
            else if (GameListBTN1.Content.ToString() == "千年戦争アイギスR(WebRender)")
            {
                GameListBTN1.Content = "あいミスR(WebRender)";

                GameListBTN2.Content = "ミナシゴノシゴト(WebRender)";

                GameListBTN3.Content = "あやかしランブル(WebRender)";

                GameListBTN4.Content = "戦国†恋姫オンラインX〜奥宴新史〜 (WebRender)";

                GameListBTN5.Content = "DeepOne 虚無と夢幻のフラグメントR(WebRender)";

                GameListBTN6.Content = "救世少女メシアガールXおかわり (WebRender)";

            }

            else if (GameListBTN1.Content.ToString() == "オトギフロンティア R(WebRender)")
            {
                GameListBTN1.Content = "千年戦争アイギスR(WebRender)";

                GameListBTN2.Content = "戦国百花伝R(WebRender)";

                GameListBTN3.Content = "対魔忍RPGX(WebRender)";

                GameListBTN4.Content = "神姫PROJECT R(WebRender)";

                GameListBTN5.Content = "モンスター娘TD〜ボクは絶海の孤島でモン娘たちに溺愛されて困っています〜X(WebRender)";

                GameListBTN6.Content = "ミストトレインガールズ〜霧の世界の車窓から〜 X(WebRender)";
            }

            else if (GameListBTN1.Content.ToString() == "アライアンスセージ")
            {
                GameListBTN1.Content = "オトギフロンティア R(WebRender)";

                GameListBTN2.Content = "アイ・アム・マジカミDX(WebRender)";

                GameListBTN3.Content = "れじぇくろ！〜レジェンド・クローバー〜 X(WebRender)";

                GameListBTN4.Content = "FLOWER KNIGHT GIRL X(WebRender)";

                GameListBTN5.Content = "エンジェリックリンクR(WebRender)";

                GameListBTN6.Content = "天啓パラドクスX(WebRender)";
            }
        }

        //次のページの処理
        private void NextPage_OnClick(object sender, RoutedEventArgs e)
        {

            if (GameListBTN1.Content.ToString() == "あいミスR(WebRender)")
            {
                GameListBTN1.Content = "千年戦争アイギスR(WebRender)";

                GameListBTN2.Content = "戦国百花伝R(WebRender)";

                GameListBTN3.Content = "対魔忍RPGX(WebRender)";

                GameListBTN4.Content = "神姫PROJECT R(WebRender)";

                GameListBTN5.Content = "モンスター娘TD〜ボクは絶海の孤島でモン娘たちに溺愛されて困っています〜X(WebRender)";

                GameListBTN6.Content = "ミストトレインガールズ〜霧の世界の車窓から〜 X(WebRender)";
            }

            else if (GameListBTN1.Content.ToString() == "千年戦争アイギスR(WebRender)")
            {
                GameListBTN1.Content = "オトギフロンティア R(WebRender)";

                GameListBTN2.Content = "アイ・アム・マジカミDX(WebRender)";

                GameListBTN3.Content = "れじぇくろ！〜レジェンド・クローバー〜 X(WebRender)";

                GameListBTN4.Content = "FLOWER KNIGHT GIRL X(WebRender)";

                GameListBTN5.Content = "エンジェリックリンクR(WebRender)";

                GameListBTN6.Content = "天啓パラドクスX(WebRender)";
            }

            else if (GameListBTN1.Content.ToString() == "オトギフロンティア R(WebRender)")
            {
                GameListBTN1.Content = "アライアンスセージ";

                GameListBTN2.Content = "プリンセスエンパイア R";

                GameListBTN3.Content = "";

                GameListBTN4.Content = "";

                GameListBTN5.Content = "";

                GameListBTN6.Content = "";
            }
            
        }

        
    }
}
