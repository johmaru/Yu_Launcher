using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using NLog;
using NLog.Config;
using Velopack;

namespace YuLauncher
{
    internal class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            var config = new XmlLoggingConfiguration("NLog.config");
            LogManager.Configuration = config;
            VelopackApp.Build().WithBeforeUninstallFastCallback((v) => {
            }).WithFirstRun((v) => {
                MessageBox.Show("Thanks for installing my application!");
            }).Run();
            var application = new App();
            application.InitializeComponent();
            application.Run();
        }
    }
}
