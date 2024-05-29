using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Threading.Tasks;
using System.Windows;
using NLog;
using YuLauncher.Core.lib;
using YuLauncher.Core.Window;
using Application = System.Windows.Application;

namespace YuLauncher
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
       public MainWindow? MainWindowInstance { get; set;}
       private DateTime _startTime;
       private readonly TomlControl _tomlControl = new();
       public LanguageUpdater? LanguageUpdater { get; set; }
        private async void Application_Startup(object sender, StartupEventArgs e)
        { 
            FirstLunch();
            LanguageCheck();
           await Initialize();
           LoggerController.LogInfo("Application Start");
        }
        
        private async Task Initialize()
        {
                  LanguageUpdater = new LanguageUpdater();
                  MainWindowInstance =　await Dispatcher.InvokeAsync(() => new MainWindow());
                 MainWindowInstance.Show();
                 _startTime = DateTime.Now;
                 LoggerController.LogInfo("App Initialize Complete");
        }
        
        private void FirstLunch()
        {
                bool path = File.Exists("./settings.toml");
                if (!path)
                {
                    TomlControl.CreateToml("./settings.toml");
                }
                bool list = File.Exists("./gameList.toml");
                if (!list)
                {
                    TomlControl.CreateGameListToml("./gameList.toml");
                }
                LoggerController.LogInfo("First Lunch Check Complete");
        }

        private void LanguageCheck()
        {
           var result = _tomlControl.GetTomlString("./settings.toml", "Language");
           switch (result)
           {
                case "en":
                    LanguageUpdater.UpdateLanguage("en-US");
                     break;
                case "ja":
                    LanguageUpdater.UpdateLanguage("ja-JP");
                     break;
           }
           LoggerController.LogInfo("Language Check Complete");
        }

        protected override void OnExit(ExitEventArgs e)
        {
            if (MainWindowInstance != null) MainWindowInstance.Close();
            LoggerController.LogInfo("Application Exit");
            base.OnExit(e);
        }
    }
}