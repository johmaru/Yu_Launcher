using System;
using System.Windows;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;
using YuLauncher.Core.lib;

namespace YuLauncher.Core.Window;

public partial class MainWindow : FluentWindow
{

    public static double WindowHeight = new MainWindow().Height;

    public MainWindow()
    {
        InitializeComponent();
        Initialize();
        WindowSizeInitialize();
        ApplicationThemeManager.Apply(this);
        LoggerController.LogInfo("MainWindow Initialized");
    }
    
    private void Initialize()
    {
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
    }
    
    private void WindowSizeInitialize()
    {
               var width = TomlControl.GetTomlString("./settings.toml", "WindowResolution", "Width");
               var height = TomlControl.GetTomlString("./settings.toml", "WindowResolution", "Height");
               Width = double.Parse(width);
               Height = double.Parse(height);
    }
    private void MetroWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        Application.Current.Shutdown();
    }

}
