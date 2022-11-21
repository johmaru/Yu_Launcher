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
          var gamewindow = new GameWindow(August_myth.Name);
           gamewindow.Show();
        }

        private void backbtn_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }
    }
}
