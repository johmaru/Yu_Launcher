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
using YuLauncher.Properties;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Drawing.Color;

namespace YuLauncher.Core.Window;

public partial class MainWindow : FluentWindow
{
    public delegate void BackBtnClickHandler(object sender, RoutedEventArgs e);
    public event BackBtnClickHandler? OnBackBtnClick;
    private TomlControl _tomlControl = new();
    public MainWindow()
    {
        InitializeComponent();
        Initialize();
        WindowSizeInitialize();
        BackBtn.Click += BackBtn_OnClick;
        ApplicationThemeManager.Apply(this);
    }
    
    private void Initialize()
    {
        this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Grid.Background = ApplicationThemeManager.GetAppTheme() == ApplicationTheme.Dark ? Brushes.DimGray : Brushes.LightGray;
    }
    
    private void WindowSizeInitialize()
    {
               var width = _tomlControl.GetTomlStringList("./settings.toml", "WindowResolution", "Width");
               var height = _tomlControl.GetTomlStringList("./settings.toml", "WindowResolution", "Height");
                Console.WriteLine(double.Parse(width));
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
        WindowStateIcon.Glyph = "\uE740";
        
    }

    private void WindowStateBtn_OnUnchecked(object sender, RoutedEventArgs e)
    {
        this.WindowState = WindowState.Normal;
        WindowStateIcon.Glyph = "\uE73F";
    }

    private void WindowStateBtn_OnLoaded(object sender, RoutedEventArgs e)
    {
        if (WindowState == WindowState.Maximized)
        {
            WindowStateIcon.Glyph = "\uE73F";
        }
        else if (WindowState == WindowState.Normal)
        {
            WindowStateIcon.Glyph = "\uE740";
        }
    }

    private void BackBtn_OnClick(object sender, RoutedEventArgs e)
    {
        OnBackBtnClick?.Invoke(sender, e);
    }
}