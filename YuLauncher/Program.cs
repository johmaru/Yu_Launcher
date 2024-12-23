using System;
using System.Collections.Generic;
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
                
                
               Thread thread = new Thread(() =>
                {
                        var app = new App();
                        app.InitializeComponent();
                        app.Run();
                });
                thread.SetApartmentState(ApartmentState.STA);
                thread.Start();
                thread.Join();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                LoggerController.LogError($"{e}");
                throw;
            }
        }
    }
}
