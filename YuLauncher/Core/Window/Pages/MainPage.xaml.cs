using System;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using Newtonsoft.Json;
using System.Windows;
using System.Windows.Controls;
using YuLauncher.Core.Window.Pages;
using YuLauncher.Properties;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace YuLauncher.Core.Pages
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
        }
        public MainPage()
        {
            InitializeComponent();

            //JsonのシリアライズはMainWindowでやってるからデシリアライズだけ

            var jsonString = File.ReadAllText(@"LoginHistory.Json");

            LoginHistory loghist = JsonSerializer.Deserialize<LoginHistory>(jsonString);

            //ラベルに最終ログイン時間を表示

            LatestLogin.Content = loghist.logindate;
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
        }
    }
}
