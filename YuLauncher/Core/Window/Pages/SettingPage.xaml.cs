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
using System.Windows.Threading;
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
        /*
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
        */
        //えっぎいバグ起こる
        /* private void DoEvents()
        {
            DispatcherFrame frame = new DispatcherFrame();
            var callback = new DispatcherOperationCallback(obj =>
            {
                ((DispatcherFrame)obj).Continue = false;
                return null;
            });
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, callback, frame);
            Dispatcher.PushFrame(frame);
        }
       */

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
            if (Settings.Default.FullScreen == false)
            {
                FullSc.IsChecked = false;
            }
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

       

        private void FCBTN_Click(object sender, RoutedEventArgs e)
        {
            string MessageTXT = "終了して変更内容を保存しますか？";
            string capTXT = "警告";
            MessageBoxButton button = MessageBoxButton.YesNo;
            MessageBoxImage icon = MessageBoxImage.Warning;
            MessageBoxResult result = MessageBox.Show(MessageTXT, capTXT, button, icon);

            switch (result)
            {

                case MessageBoxResult.Yes:
                    var settings = Settings.Default;
                    double HD = Convert.ToDouble(TXTRES1.Text);
                    double WD = Convert.ToDouble(TXTRES2.Text);
                    settings.Window_W = WD;
                    settings.Window_H = HD;
                    settings.Save();
                    NavigationService.GoBack();
                    break;

                case MessageBoxResult.No:
                    break;
            }
        }
    }
}
