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
using YuLauncher.Core.Pages;
using YuLauncher.Core.Window.Pages;
using YuLauncher.Properties;

namespace YuLauncher.Core.Pages
{
    /// <summary>
    /// MainPage.xaml の相互作用ロジック
    /// </summary>
    public partial class MainPage : Page
    {
        public MainPage()
        {
            InitializeComponent();
           
        }

        private void SettingBTN_OnClick(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new SettingPage());
        }

        private void StartBTN_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new GameListPage());
        }
    }
}
