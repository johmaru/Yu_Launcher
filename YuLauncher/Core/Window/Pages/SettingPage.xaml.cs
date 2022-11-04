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
using YuLauncher.Properties;

namespace YuLauncher.Core.Pages
{
    /// <summary>
    /// SettingPage.xaml の相互作用ロジック
    /// </summary>
    public partial class SettingPage : Page
    {
        public SettingPage()
        {
            InitializeComponent();
        }

        void WindowSizeSave()
        {
            var settings = Settings.Default;
            double HD = Convert.ToDouble(TXTRES1.Text);
            double WD = Convert.ToDouble(TXTRES2.Text);
            settings.Window_W = WD;
            settings.Window_H = HD;
            settings.Save();
        }
        private void FullScBTN()
        {
            string MessageTXT = "変更を適用するには再起動する必要があります、再起動しますか？";
            string capTXT = "警告";
            MessageBoxButton button = MessageBoxButton.YesNo;
            MessageBoxImage icon = MessageBoxImage.Warning;
            MessageBoxResult result = MessageBox.Show(MessageTXT, capTXT, button, icon);

            switch (result)
            {

                case MessageBoxResult.Yes:
                    Application.Current.Shutdown();
                    break;

                case MessageBoxResult.No:
                    break;
            }
        }


        private void FullSc_OnChecked(object sender, RoutedEventArgs e)
        {
            Settings.Default.FullScreen = true;
            Settings.Default.Save();
        }

        private void FullSc_OnUnchecked(object sender, RoutedEventArgs e)
        {
            Settings.Default.FullScreen = false;
            Settings.Default.Save();
        }

        private void FullSc_OnLoaded(object sender, RoutedEventArgs e)
        {
            if (Settings.Default.FullScreen == true)
            {
                FullSc.IsChecked = true;
            }
            else
            {
                FullSc.IsChecked = false;
            }
        }

        private void FullSc_OnClick(object sender, RoutedEventArgs e)
        {
            FullScBTN();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            WindowSizeSave();
        }

        private void TXTRES1_Loaded(object sender, RoutedEventArgs e)
        {
            Double txt1 = Settings.Default.Window_H;

            String txt1ST = txt1.ToString();

            TXTRES1.Text = txt1ST;
        }

        private void TXTRES2_Loaded(object sender, RoutedEventArgs e)
        {
            double txt2 = Settings.Default.Window_W;

            String txt2ST = txt2.ToString();

            TXTRES2.Text = txt2ST;
        }
    }
}
