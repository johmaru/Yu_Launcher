using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Velopack;

namespace YuLauncher
{
    internal class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            VelopackApp.Build().WithBeforeUninstallFastCallback((v) => {
                // delete / clean up some files before uninstallation
            }).WithFirstRun((v) => {
                MessageBox.Show("Thanks for installing my application!");
            }).Run();
            var application = new App();
            application.InitializeComponent();
            application.Run();
        }
    }
}
