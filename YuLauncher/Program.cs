using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using NLog;
using NLog.Config;
using Velopack;
using YuLauncher.Core.lib;
using YuLauncher.Game.Window;

namespace YuLauncher
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            try
            {
                var config = new XmlLoggingConfiguration("NLog.config");
                LogManager.Configuration = config;
                VelopackApp.Build().WithBeforeUninstallFastCallback((v) => { }).WithFirstRun((v) =>
                {
                    MessageBox.Show(LocalizeControl.GetLocalize<string>("InstallComplete"));
                }).Run();
                
                
             
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                LoggerController.LogError($"{e}");
                throw;
            }
        }

        private static async void TempCheckStart()
        {
            try
            {
                await Temp();
                await ThreadStart();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                LoggerController.LogError($"{e}");
                throw;
            }
        }

        private static ValueTask Temp()
        {
            string tempPath = Path.Combine("..", "Temp");
            string fullPath = Path.GetFullPath(tempPath);
            
            if (Directory.Exists(fullPath))
            {
                string webviewData = Path.Combine(fullPath, "YuLauncher.exe.WebView2");
                string webviewDataFullPath = Path.GetFullPath(webviewData);
                
                Directory.Move(webviewDataFullPath, "YuLauncher.exe.WebView2");
                
                string games = Path.Combine(fullPath, "Games");
                string gamesFullPath = Path.GetFullPath(games);
                
                Directory.Move(gamesFullPath, "Games");
                
                string html = Path.Combine(fullPath, "html");
                string htmlFullPath = Path.GetFullPath(html);
                
                Directory.Move(htmlFullPath, "html");
                
                string settings = Path.Combine(fullPath, "settings.toml");
                string settingsFullPath = Path.GetFullPath(settings);
                
                File.Move(settingsFullPath, "settings.toml");
                
                Directory.Delete(fullPath);
            }
            return ValueTask.CompletedTask;
        }
        
        private static ValueTask ThreadStart()
        {
            Thread thread = new Thread(() =>
            {
                var app = new App();
                app.InitializeComponent();
                app.Run();
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            return ValueTask.CompletedTask;
        }
    }
}
