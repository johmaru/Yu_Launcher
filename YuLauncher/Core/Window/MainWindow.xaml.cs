using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using System.Windows;
using System.Windows.Media;
using ControlzEx.Standard;
using MahApps.Metro.Controls;
using YuLauncher.Properties;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace YuLauncher.Core.Window;

public partial class MainWindow : MetroWindow
{
    //Jsonの中身
    /*internal class LoginHistory
    {
       [JsonPropertyName("LatestLogin")]
       public string? logindate { get; set; }
    }
    */
    

    public MainWindow()
    {
        InitializeComponent();

        this.WindowStartupLocation = WindowStartupLocation.CenterOwner;

        /*Jsonオプション

        var option = new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),

            WriteIndented = true
        };
            
        Settings.Default.LoginDete = DateTime.Today.ToString("d");

       var loginstrek = Settings.Default.LoginStreek;

       Settings.Default.ProgramLaunchCount = ++Settings.Default.ProgramLaunchCount;
       Settings.Default.Save();

       //ココらへんからログインストリーク機能追加
       
       long num = ++loginstrek;

       var zikan = Settings.Default.LoginDete;

       //アプリケーションが起動した時に起動回数と起動時間をjsonに保存

        var loginhistory = new LoginHistory
        {
            logindate = zikan,
        };

        

        //ここJson関係

        var jsonString = JsonSerializer.Serialize(loginhistory, option);
        */
        /*
        Settings.Default.LoginDete = DateTime.Today.ToString("d");

        var zikan = Settings.Default.LoginDete;

        if (File.Exists("LoginHistory.txt"))

        {
            
            StreamWriter writer = new StreamWriter("./a.txt", true);
            writer.WriteLine(zikan);
            
        }

        else { FileStream fs = File.Create("./LoginHistory.txt"); }

        /*いらん

        var JsonStrin = File.ReadAllText(@"LoginHistory.Json");

        LoginHistory loghis = JsonSerializer.Deserialize<LoginHistory>(JsonStrin);

        var filename = loghis.logindate;
        
        

        var Year = DateTime.Now.Year;

        var Month = DateTime.Now.Month;

        var Day = DateTime.Now.Day;

       
        */
        //ここからログイン履歴が見れる機能を追加




        var loginstrek = Settings.Default.LoginStreek;

        long num = ++loginstrek;

       

    }

    //RecoverWindowSizeはまだ使うか未定.というのもWindow_WとWindow_Hはゲームスクリーンの解像度の値にする予定
    //MainWindow用のSettingの値を作成する可能性が微レ存
    void RecoverWindowSize()
    {
        var settings = Settings.Default;
        if (settings.Window_W > 0 &&
            settings.Window_W <= SystemParameters.WorkArea.Width)
        {
            Width = settings.Window_W;
        }

        if (settings.Window_H > 0 &&
            settings.Window_H <= SystemParameters.WorkArea.Height)
        {
            Height = settings.Window_H;
        }
    }

 

    public void MWClose()
    {
        this.Close();
    }

    private void MetroWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
       
    }
}