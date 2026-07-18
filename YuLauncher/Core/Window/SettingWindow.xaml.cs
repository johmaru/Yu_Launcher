using System;
using System.Threading.Tasks;
using System.Windows;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;
using YuLauncher.Core.lib;

namespace YuLauncher.Core.Window;

public partial class SettingWindow : FluentWindow
{
    private readonly ManualTomlSettings _manualTomlSettings = new();

    public SettingWindow()
    {
        InitializeComponent();
        ThemeInitialize();
        Task.CompletedTask.Wait();
        GetResolution();
        LoggerController.LogInfo("Setting Window Initialized!");
    }
    
    private void ThemeInitialize()
    {
         Task.Run(() =>
        {
            switch (TomlControl.GetTomlString("./settings.toml", "Theme"))
            {
                case "Dark":
                    ThemeApply("Dark");
                    break;
                case "Light":
                    ThemeApply("Light");
                    break;
            }
        });
    }
    
    private void ThemeApply(string theme)
    {
        Task.Run(() =>
        {
            switch (theme)
            {
                case "Dark":
                    Dispatcher.InvokeAsync(() => ApplicationThemeManager.Apply(ApplicationTheme.Dark));
                    break;
                case "Light":
                    Dispatcher.InvokeAsync(() => ApplicationThemeManager.Apply(ApplicationTheme.Light));
                    break;
            }
        });
    }
    
    private void GetResolution()
    {
           var width = ManualTomlSettings.GetSettingWindowResolution("./settings.toml", "SettingResolution", "Width");
           var height = ManualTomlSettings.GetSettingWindowResolution("./settings.toml", "SettingResolution", "Height");
           
            Width = double.Parse(width);
            Height = double.Parse(height);
    }

}
