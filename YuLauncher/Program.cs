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
        [STAThread]
        public static void Main(string[] args)
        {
            try
            {
                // NLog must be configured before any logger is used.
                var config = new XmlLoggingConfiguration("NLog.config");
                LogManager.Configuration = config;

                DocumentCheckStart();
                TempCheckStart();

                VelopackApp.Build().OnBeforeUninstallFastCallback((v) => { }).OnFirstRun((v) =>
                {
                    MessageBox.Show(LocalizeControl.GetLocalize<string>("InstallComplete"));
                }).Run();

                var app = new App();
                app.InitializeComponent();
                app.Run();
            }
            catch (Exception e)
            {
                LoggerController.LogError($"{e}");
                throw;
            }
        }

        private static void DocumentCheckStart()
        {
            try
            {
                string documentsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "YuLauncher");
                string fullPath = Path.GetFullPath(documentsPath);
                if (!Directory.Exists(fullPath))
                {
                    CreateDocuments();
                }
            }
            catch (Exception e)
            {
                LoggerController.LogError($"{e}");
                throw;
            }
        }

        private static void CreateDocuments()
        {
            string documentsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "YuLauncher");
            string fullPath = Path.GetFullPath(documentsPath);
            if (!Directory.Exists(fullPath))
            {
                Directory.CreateDirectory(fullPath);
            }
        }

        private static void TempCheckStart()
        {
            try
            {
                Temp();
            }
            catch (Exception e)
            {
                LoggerController.LogError($"{e}");
                throw;
            }
        }

        private static void Temp()
        {
            string tempPath = Path.Combine("..", "Temp");
            string fullPath = Path.GetFullPath(tempPath);

            if (Directory.Exists(fullPath))
            {
                string webviewData = Path.Combine(fullPath, "YuLauncher.exe.WebView2");
                string webviewDataFullPath = Path.GetFullPath(webviewData);

                if (Directory.Exists("YuLauncher.exe.WebView2"))
                {
                    Directory.Delete(webviewDataFullPath, true);
                }
                else
                {
                    Directory.Move(webviewDataFullPath, "YuLauncher.exe.WebView2");
                }

                string games = Path.Combine(fullPath, "Games");
                string gamesFullPath = Path.GetFullPath(games);

                if (Directory.Exists("Games"))
                {
                    Directory.Delete(gamesFullPath, true);
                }
                else
                {
                    Directory.Move(gamesFullPath, "Games");
                }

                string html = Path.Combine(fullPath, "html");
                string htmlFullPath = Path.GetFullPath(html);

                if (Directory.Exists("html"))
                {
                    Directory.Delete(htmlFullPath, true);
                }
                else
                {
                    Directory.Move(htmlFullPath, "html");
                }

                string settings = Path.Combine(fullPath, "settings.toml");
                string settingsFullPath = Path.GetFullPath(settings);

                if (File.Exists("settings.toml"))
                {
                    File.Delete(settingsFullPath);
                }
                else
                {
                    File.Move(settingsFullPath, "settings.toml");
                }

                Directory.Delete(fullPath);
            }
        }
    }
}
