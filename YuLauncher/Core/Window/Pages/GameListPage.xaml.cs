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

        private void BackPage_OnClick(object sender, RoutedEventArgs e)
        {
            if (GameListBTN1.IsLoaded == true)
            {
                MessageBox.Show("これ以上後ろのページは有りません");
            }
            else if (GameListBTN1.Content.ToString() == "千年戦争アイギスR")
            {
                GameListBTN1.Content = "あいりすミスティリア!R";

            }
        }

        private void NextPage_OnClick(object sender, RoutedEventArgs e)
        {
            if (GameListBTN1.IsLoaded == true)
            {
                GameListBTN1.Name = "Aigisu";
                GameListBTN1.Content = "千年戦争アイギスR";
            }
        }
    }
}
