using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Controls;
using MahApps.Metro.Controls.Dialogs;
using YuLauncher.Properties;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace YuLauncher.Core.Window.Pages
{
    /// <summary>
    /// MainPage.xaml の相互作用ロジック
    /// </summary>
    public partial class MainPage : Page
    {
        internal class LoginHistory
        {
            //Json中身
            [JsonPropertyName("LatestLogin")]
            public string? logindate { get; set; }

            [JsonPropertyName("No")] public long? number { get; set; }

            public string TodaysLogin { get; set; } = "";
        }
        public MainPage()
        {
            InitializeComponent();

            LoginTo();

            var option = new JsonSerializerOptions()
            {
                AllowTrailingCommas = true,
            };

            /*JsonのシリアライズはMainWindowでやってるからデシリアライズだけ

            var jsonString = File.ReadAllText(@"LoginHistory.Json");

            LoginHistory loghist = JsonSerializer.Deserialize<LoginHistory>(jsonString, option);
            */

            //ラベルに最終ログイン時間を表示

           //var txtString = File.ReadAllText(@"LoginHistory.txt");
           

           //LatestLogin.Content = txtString;
        }

        public void LoginTo()
        {
            DateTime dt = DateTime.Now;

            string dtresult = dt.ToString("yyyy/MM/dd");

            string kyou = Settings.Default.TodaysLogin;

            LatestLogin.Content = dtresult;

            bool syokai = Settings.Default.TodaysYesNo = false;


            if (Directory.Exists("Data/LoginData"))
            {
                if (kyou != dtresult)
                {

                    TodaysLogin.Content = "今日は初めての起動です";

                    Settings.Default.TodaysLogin = dtresult;

                    Settings.Default.Save();
                }


                else if (Settings.Default.TodaysLogin == dtresult)
                {
                    TodaysLogin.Content = $"{dtresult}今日はランチャーにログイン済みです";
                }
            }
            else
            {
                Directory.CreateDirectory("Data/LoginData");

                MessageBox.Show("初回起動です");

                TodaysLogin.Content = "初回起動です、ログイン確認機能は次の起動から機能します";

                Settings.Default.Reset();
            }

           

        }

        private void SettingBTN_OnClick(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new SettingPage());
        }

        private void StartBTN_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new GameListPage());
        }

        private void LoginHistoryBTN_OnClick(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new GameHistoryPage());
        }
    }
}
