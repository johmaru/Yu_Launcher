using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;
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
using ControlzEx.Standard;
using MahApps.Metro.Controls;
using Newtonsoft.Json;
using YuLauncher.Core.Pages;
using YuLauncher.Game.Window;
using YuLauncher.Properties;
using YuLauncher.Core.Window.Pages;
using JsonSerializer = System.Text.Json.JsonSerializer;
using Path = System.IO.Path;

namespace YuLauncher;

public partial class MainWindow : MetroWindow
{
    //Jsonの中身
    internal class LoginHistory
    {
       [JsonPropertyName("LatestLogin")]
       public string? logindate { get; set; }

       [JsonPropertyName("No")] public long? number { get; set; }
    }
    

    public MainWindow()
    {
        InitializeComponent();
        
        this.WindowStartupLocation = WindowStartupLocation.CenterOwner;

        //Jsonオプション

        var option = new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),

            WriteIndented = true
        };
            
        Settings.Default.LoginDete = DateTime.Now.ToString();

       var loginstrek = Settings.Default.LoginStreek;

       Settings.Default.ProgramLaunchCount = ++Settings.Default.ProgramLaunchCount;
       Settings.Default.Save();

       //ココらへんからログインストリーク機能追加
       
       long num = ++loginstrek;

       var zikan = Settings.Default.LoginDete;

       //アプリケーションが起動した時に起動回数と起動時間をjsonに保存

        var loginhistory = new LoginHistory
        {
            logindate = Settings.Default.LoginDete
        };

        //ここJson関係

        var jsonString = JsonSerializer.Serialize(loginhistory, option);

        File.WriteAllText(@"LoginHistory.Json", jsonString);

        var JsonStrin = File.ReadAllText(@"LoginHistory.Json");

        LoginHistory loghis = JsonSerializer.Deserialize<LoginHistory>(JsonStrin);

        var filename = loghis.logindate;

        var Year = DateTime.Now.Year;

        var Month = DateTime.Now.Month;

        var Day = DateTime.Now.Day;

        if (Directory.Exists("Data/LoginData"))
        {
            FileInfo logdataInfo = new FileInfo($"Data/LoginData/{Year},{Month},{Day}");

            FileStream fs = logdataInfo.Create();

            fs.Close();
        }
        else
        {
            Directory.CreateDirectory("Data/LoginData");

            FileInfo logdataInfo = new FileInfo($"Data/LoginData/{Year}{Month}{Day}");

            FileStream fs = logdataInfo.Create();

            fs.Close();
        }

        //ここからログイン履歴が見れる機能を追加


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
    
    
}