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
using Velopack;
using Velopack.Locators;
using Velopack.Sources;
using YuLauncher.Core.lib;
using YuLauncher.Core.Window;
using Application = System.Windows.Application;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace YuLauncher
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
       public MainWindow? MainWindowInstance { get; set;}
       public LanguageUpdater? LanguageUpdater { get; set; }
        private async void Application_Startup(object sender, StartupEventArgs e)
        { 
            try
            {
                await FirstLunch();

                await LanguageCheck();

                await Initialize();

                await JsonCheck();

                _ = UpdateCheck();
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                LoggerController.LogError($"{exception}");
                throw;
            }
           
           LoggerController.LogInfo("Application Start");
        }
        
        private async ValueTask Initialize()
        {
                  LanguageUpdater = new LanguageUpdater();
                  MainWindowInstance =　await Dispatcher.InvokeAsync(() => new MainWindow());
                 MainWindowInstance.Show();
                 LoggerController.LogInfo("App Initialize Complete");
        }

        private static async Task UpdateCheck()
        {
           
            try
            {
                var mgr = new UpdateManager(new GithubSource(@"https://github.com/johmaru/Yu_Launcher", null, false),
                    new UpdateOptions
                    {
                        AllowVersionDowngrade = true
                    });

                if (!mgr.IsInstalled)
                {
                    LoggerController.LogError("Application is not installed. Update check cannot proceed.");
                    return;
                }

                var newVersionCheck = await mgr.CheckForUpdatesAsync();
                if (newVersionCheck == null) return;

                var result = MessageBox.Show(LocalizeControl.GetLocalize<string>("NewVersionAb"), "Update", MessageBoxButton.YesNo);
                if (result == MessageBoxResult.Yes)
                {
                    await mgr.DownloadUpdatesAsync(newVersionCheck);
                    mgr.ApplyUpdatesAndRestart(newVersionCheck);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                LoggerController.LogError($"{e}");
                throw;
            }
        }
        
        private static ValueTask FirstLunch()
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
                bool html = Directory.Exists("./html");
                if (!html)
                {
                    Directory.CreateDirectory("./html");
                }

                if (!File.Exists("./Games"))
                {
                    Directory.CreateDirectory("./Games");
                }
                LoggerController.LogInfo("First Lunch Check Complete");
                return ValueTask.CompletedTask;
        }

        private static async ValueTask JsonCheck()
        {
            var json = Directory.GetFiles("./Games", "*.json");
            foreach (var t in json)
            {
                var data = await JsonControl.ReadExeJson(t);
                await JsonControl.CheckJsonData(t, data);
            }
            LoggerController.LogInfo("Json Check Complete");
        }

        private static ValueTask LanguageCheck()
        {
           var result = TomlControl.GetTomlString("./settings.toml", "Language");
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
           return ValueTask.CompletedTask;
        }

        protected override void OnExit(ExitEventArgs e)
        {
            if (MainWindowInstance != null) MainWindowInstance.Close();
            LoggerController.LogInfo("Application Exit");
            base.OnExit(e);
            Current.Shutdown();
        }
    }
}