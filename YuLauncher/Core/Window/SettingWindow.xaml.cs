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
    private readonly ManualTomlSettings _manualTomlSettings = new();
    public static double WindowWidth = new SettingWindow().Width;
    public SettingWindow()
    {
        InitializeComponent();
        ThemeInitialize();
        Initialize();
        Task.CompletedTask.Wait();
        GetResolution();
        LoggerController.LogInfo("Setting Window Initialized!");
    }
    
    private void Initialize()
    {
        Grid.Background = ApplicationThemeManager.GetAppTheme() == ApplicationTheme.Dark ? Brushes.DimGray : Brushes.LightGray;
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
           var width = _manualTomlSettings.GetSettingWindowResolution("./settings.toml", "SettingResolution", "Width");
           var height = _manualTomlSettings.GetSettingWindowResolution("./settings.toml", "SettingResolution", "Height");
           
            this.Width = double.Parse(width);
            this.Height = double.Parse(height);
    }

    private void ExitBtn_OnClick(object sender, RoutedEventArgs e)
    {
       this.Close();
       LoggerController.LogInfo("Setting Window Closed!");
    }

    private void SettingWindow_OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
            this.DragMove();
    }

    private void WindowStateBtn_OnChecked(object sender, RoutedEventArgs e)
    {
        this.WindowState = WindowState.Maximized;
        WindowStateIcon.Glyph = "\uE73F";
    }

    private void WindowStateBtn_OnUnchecked(object sender, RoutedEventArgs e)
    {
        this.WindowState = WindowState.Normal;
        WindowStateIcon.Glyph = "\uE740";
    }

    private void SettingWindow_OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        switch (WindowState)
        {
            case WindowState.Maximized:
                WindowStateIcon.Glyph = "\uE73F";
                WindowStateBtn.IsChecked = true;
                break;
            case WindowState.Normal:
                WindowStateIcon.Glyph = "\uE740";
                WindowStateBtn.IsChecked = false;
                break;
            case WindowState.Minimized:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void Grid_OnMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            if (this.WindowState == WindowState.Maximized)
            {
                var point = Mouse.GetPosition(this);
                this.WindowState = WindowState.Normal;
                this.Left = point.X - this.Width / 2;
                this.Top = point.Y;
                this.DragMove();
            }
        }
    }

    private void MinimizeBtn_OnClick(object sender, RoutedEventArgs e)
    {
        this.WindowState = WindowState.Minimized;
    }
}