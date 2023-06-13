using System;
using System.Collections.Generic;
using System.IO;
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
           Application.Current.MainWindow.Hide();
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
        public void FavBTN_ClickEvent(object sender, RoutedEventArgs e)
        {
        }

        private void MenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            var rootPath = AppDomain.CurrentDomain.BaseDirectory;

            string config = "config";

            var dirPath = rootPath + config;

            string textPath = "./Favorite.txt";

            var textDir = dirPath + textPath;

            //これ書かないとなぜかJIS使えない
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            using (StreamWriter writer = new StreamWriter(textDir, true, Encoding.GetEncoding("Shift_JIS")))
            {
                writer.WriteLine(GameListBTN1.Content);
            }

            MessageBox.Show(GameListBTN1.Content + "をお気に入りのゲームに追加しました");
        }

        private void GameButtonCtx1_Loaded(object sender, RoutedEventArgs e)
        {
            GameButtonCtx1.Header = GameListBTN1.Content + "をお気に入りに登録します";
        }

        private void GameButtonCtx2_OnClick(object sender, RoutedEventArgs e)
        {
            var rootPath = AppDomain.CurrentDomain.BaseDirectory;

            string config = "config";

            var dirPath = rootPath + config;

            string textPath = "./Favorite.txt";

            var textDir = dirPath + textPath;

            //これ書かないとなぜかJIS使えない
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            using (StreamWriter writer = new StreamWriter(textDir, true, Encoding.GetEncoding("Shift_JIS")))
            {
                writer.WriteLine(GameListBTN2.Content);
            }

            MessageBox.Show(GameListBTN2.Content + "をお気に入りのゲームに追加しました");
        }

        private void GameButtonCtx2_OnLoaded(object sender, RoutedEventArgs e)
        {
            GameButtonCtx2.Header = GameListBTN2.Content + "をお気に入りに登録します";
        }

        private void GameButtonCtx3_OnClick(object sender, RoutedEventArgs e)
        {
            var rootPath = AppDomain.CurrentDomain.BaseDirectory;

            string config = "config";

            var dirPath = rootPath + config;

            string textPath = "./Favorite.txt";

            var textDir = dirPath + textPath;

            //これ書かないとなぜかJIS使えない
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            using (StreamWriter writer = new StreamWriter(textDir, true, Encoding.GetEncoding("Shift_JIS")))
            {
                writer.WriteLine(GameListBTN3.Content);
            }

            MessageBox.Show(GameListBTN3.Content + "をお気に入りのゲームに追加しました");
        }

        private void GameButtonCtx3_OnLoaded(object sender, RoutedEventArgs e)
        {
            GameButtonCtx3.Header = GameListBTN3.Content + "をお気に入り登録します";
        }

        private void GameButtonCtx4_OnClick(object sender, RoutedEventArgs e)
        {
            var rootPath = AppDomain.CurrentDomain.BaseDirectory;

            string config = "config";

            var dirPath = rootPath + config;

            string textPath = "./Favorite.txt";

            var textDir = dirPath + textPath;

            //これ書かないとなぜかJIS使えない
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            using (StreamWriter writer = new StreamWriter(textDir, true, Encoding.GetEncoding("Shift_JIS")))
            {
                writer.WriteLine(GameListBTN4.Content);
            }

            MessageBox.Show(GameListBTN4.Content + "をお気に入りのゲームに追加しました");
        }

        private void GameButtonCtx4_OnLoaded(object sender, RoutedEventArgs e)
        {
            GameButtonCtx4.Header = GameListBTN4.Content + "をお気に入り登録します";
        }

        private void GameButtonCtx5_OnClick(object sender, RoutedEventArgs e)
        {
            var rootPath = AppDomain.CurrentDomain.BaseDirectory;

            string config = "config";

            var dirPath = rootPath + config;

            string textPath = "./Favorite.txt";

            var textDir = dirPath + textPath;

            //これ書かないとなぜかJIS使えない
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            using (StreamWriter writer = new StreamWriter(textDir, true, Encoding.GetEncoding("Shift_JIS")))
            {
                writer.WriteLine(GameListBTN5.Content);
            }

            MessageBox.Show(GameListBTN5.Content + "をお気に入りのゲームに追加しました");
        }

        private void GameButtonCtx5_OnLoaded(object sender, RoutedEventArgs e)
        {
            GameButtonCtx5.Header = GameListBTN5.Content + "をお気に入りに登録します";
        }


        private void GameButtonCtx6_OnClick(object sender, RoutedEventArgs e)
        {
            var rootPath = AppDomain.CurrentDomain.BaseDirectory;

            string config = "config";

            var dirPath = rootPath + config;

            string textPath = "./Favorite.txt";

            var textDir = dirPath + textPath;

            //これ書かないとなぜかJIS使えない
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            using (StreamWriter writer = new StreamWriter(textDir, true, Encoding.GetEncoding("Shift_JIS")))
            {
                writer.WriteLine(GameListBTN6.Content);
            }

            MessageBox.Show(GameListBTN6.Content + "をお気に入りのゲームに追加しました");
        }

        private void GameButtonCtx6_OnLoaded(object sender, RoutedEventArgs e)
        {
            GameButtonCtx6.Header = GameListBTN6.Content + "をお気に入りに登録します";
        }
    }
    }
