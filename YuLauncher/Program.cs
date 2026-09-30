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
            string fullPath = Path.GetFullPath(Path.Combine("..", "Temp"));
            if (!Directory.Exists(fullPath))
            {
                return;
            }

            string webviewData = Path.Combine(fullPath, "YuLauncher.exe.WebView2");
            string games = Path.Combine(fullPath, "Games");
            string html = Path.Combine(fullPath, "html");
            string settings = Path.Combine(fullPath, "settings.toml");

            if (Directory.Exists(webviewData))
            {
                FileControl.CopyDirectory(webviewData, "YuLauncher.exe.WebView2", overwrite: false);
            }
            if (Directory.Exists(games))
            {
                FileControl.CopyDirectory(games, "Games", overwrite: false);
            }
            if (Directory.Exists(html))
            {
                FileControl.CopyDirectory(html, "html", overwrite: false);
            }
            if (File.Exists(settings) && !File.Exists("settings.toml"))
            {
                File.Copy(settings, "settings.toml", false);
            }

            // Delete known backup entries only after every restore operation succeeds.
            if (Directory.Exists(webviewData)) Directory.Delete(webviewData, true);
            if (Directory.Exists(games)) Directory.Delete(games, true);
            if (Directory.Exists(html)) Directory.Delete(html, true);
            if (File.Exists(settings)) File.Delete(settings);
            if (!Directory.EnumerateFileSystemEntries(fullPath).Any())
            {
                Directory.Delete(fullPath);
            }
            else
            {
                LoggerController.LogWarn($"Update backup contains unrecognized entries and was retained: {fullPath}");
            }
        }
    }
}
