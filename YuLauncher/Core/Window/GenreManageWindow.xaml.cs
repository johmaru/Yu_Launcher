using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;
using YuLauncher.Core.lib;
using MessageBox = System.Windows.MessageBox;

namespace YuLauncher.Core.Window;

public partial class GenreManageWindow : FluentWindow
{
   private  JsonControl.ApplicationJsonData Data { get; set; }
    public GenreManageWindow(JsonControl.ApplicationJsonData data)
    {
        InitializeComponent();
        
        ApplicationThemeManager.Apply(this);
        
        Data = data;
        
       Initialize();
    }
    
    private void Initialize()
    {
        foreach (var genre in Data.Genre)
        {
            CheckBox checkBox = new()
            {
                Content = genre,
                Margin = new Thickness(5),
                IsChecked = Data.Genre.Contains(genre)
            };
            checkBox.Checked += async (_, _) =>
            {
                string[] newGenre = Data.Genre.ToList().Append(checkBox.Content.ToString()).ToArray()!;
                Data = Data with { Genre = newGenre };
                await   JsonControl.CreateExeJson(Data.JsonPath, Data);
            };
            checkBox.Unchecked +=async (_, _) =>
            {
                var newGenre = Data.Genre.ToList().Where(x => x != checkBox.Content.ToString()).ToArray();
                Data = Data with { Genre = newGenre };
                await   JsonControl.CreateExeJson(Data.JsonPath, Data);
            };
            
            WrapPanel.Children.Add(checkBox);
        }
    }

    private async ValueTask RefreshContents()
    {
        if (JsonControl.ReadExeJson(Data.JsonPath).IsCompleted)
        {
            Data = JsonControl.ReadExeJson(Data.JsonPath).Result;
        }
        else
        {
            Data = await JsonControl.ReadExeJson(Data.JsonPath);
        }
        WrapPanel.Children.Clear();
        foreach (var genre in Data.Genre)
        {
            CheckBox checkBox = new()
            {
                Content = genre,
                Margin = new Thickness(5),
                IsChecked = Data.Genre.Contains(genre)
            };
            checkBox.Checked += async (_, _) =>
            {
                string[] newGenre = Data.Genre.ToList().Append(checkBox.Content.ToString()).ToArray()!;
                Data = Data with { Genre = newGenre };
             await   JsonControl.CreateExeJson(Data.JsonPath, Data);
            };
            checkBox.Unchecked +=async (_, _) =>
            {
                var newGenre = Data.Genre.ToList().Where(x => x != checkBox.Content.ToString()).ToArray();
                Data = Data with { Genre = newGenre };
             await   JsonControl.CreateExeJson(Data.JsonPath, Data);
            };
            
            WrapPanel.Children.Add(checkBox);
        }
    }

    private async void AddButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(GenreText.Text) || JsonControl.CheckAppDataContent(Data.Genre, GenreText.Text).Result)
        {
            MessageBox.Show(LocalizeControl.GetLocalize<string>("GenreNameInput"));
            return;
        }

        var newGenre = Data.Genre.ToList().Append(GenreText.Text).ToArray();
        Data = Data with { Genre = newGenre };
        await JsonControl.CreateExeJson(Data.JsonPath, Data);
        
        await RefreshContents();
        MessageBox.Show(LocalizeControl.GetLocalize<string>("GenreAdd"));
    }

    private void Grid_OnMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed) return;
        if (WindowState != WindowState.Maximized) return;
        var point = Mouse.GetPosition(this);
        WindowState = WindowState.Normal;
        Left = point.X - Width / 2;
        Top = point.Y;
        DragMove();
    }

    private void MinimizeBtn_OnClick(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void WindowStateBtn_OnChecked(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Maximized;
        WindowStateIcon.Glyph = "\uE73F";
    }

    private void WindowStateBtn_OnUnchecked(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Normal;
        WindowStateIcon.Glyph = "\uE740";
    }

    private void ExitBtn_OnClick(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void GenreManageWindow_OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
            DragMove();
    }
}