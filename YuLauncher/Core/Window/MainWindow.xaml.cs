using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;
using YuLauncher.Core.lib;
using YuLauncher.Core.Window.Pages;
using YuLauncher.Properties;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Drawing.Color;

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
        this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Grid.Background = ApplicationThemeManager.GetAppTheme() == ApplicationTheme.Dark ? Brushes.DimGray : Brushes.LightGray;
    }
    
    private void WindowSizeInitialize()
    {
               var width = TomlControl.GetTomlString("./settings.toml", "WindowResolution", "Width");
               var height = TomlControl.GetTomlString("./settings.toml", "WindowResolution", "Height");
               this.Width = double.Parse(width);
               this.Height = double.Parse(height);
    }
    private void MetroWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
       
    }

    private void ExitBtn_OnClick(object sender, RoutedEventArgs e)
    {
       this.Close();
    }

    private void MainWindow_OnMouseDown(object sender, MouseButtonEventArgs e)
    {
       if(e.ChangedButton == MouseButton.Left)
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

    private void MainWindow_OnSizeChanged(object sender, SizeChangedEventArgs e)
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
        switch (e.LeftButton)
        {
            case MouseButtonState.Pressed:
            {
                if (this.WindowState == WindowState.Maximized)
                {
                    var point = Mouse.GetPosition(this);
                    this.WindowState = WindowState.Normal;
                    this.Left = point.X - this.Width / 2;
                    this.Top = point.Y;
                    this.DragMove();
                }

                break;
            }
            case MouseButtonState.Released:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void MinimizeBtn_OnClick(object sender, RoutedEventArgs e)
    {
        this.WindowState = WindowState.Minimized;
    }
}