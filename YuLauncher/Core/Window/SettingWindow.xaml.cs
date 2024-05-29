using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;
using YuLauncher.Core.lib;

namespace YuLauncher.Core.Window;

public partial class SettingWindow : FluentWindow
{
   private TomlControl _tomlControl = new();
   private ManualTomlSettings _manualTomlSettings = new();
    public SettingWindow()
    {
        InitializeComponent();
        ThemeInitialize();
        Initialize();
        Task.CompletedTask.Wait();
        GetResolution();
    }
    
    private void Initialize()
    {
        Grid.Background = ApplicationThemeManager.GetAppTheme() == ApplicationTheme.Dark ? Brushes.DimGray : Brushes.LightGray;
    }
    private void ThemeInitialize()
    {
         Task.Run(() =>
        {
            switch (_tomlControl.GetTomlString("./settings.toml", "Theme"))
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
           var width = _manualTomlSettings.GetSettingWindowResolution("./settings.toml", "SettingResolution", "Width");
           var height = _manualTomlSettings.GetSettingWindowResolution("./settings.toml", "SettingResolution", "Height");
           
            this.Width = double.Parse(width);
            this.Height = double.Parse(height);
    }

    private void ExitBtn_OnClick(object sender, RoutedEventArgs e)
    {
       this.Close();
    }

    private void SettingWindow_OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
            this.DragMove();
    }
}