using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;
using YuLauncher.Core.lib;
using Button = Wpf.Ui.Controls.Button;
using TextBlock = Wpf.Ui.Controls.TextBlock;
using TextBox = Wpf.Ui.Controls.TextBox;

namespace YuLauncher.Core.Window;

public partial class MemoWindow : FluentWindow
{
    private readonly string[] _memo;
    private readonly string _filePath;
    private readonly double _fontSize;
    public MemoWindow(string filePath,string[] memo)
    {
        this._memo = memo;
        this._filePath = filePath;
        this._fontSize = double.Parse(TomlControl.GetTomlString("./settings.toml", "MemoFontSize"));
        InitializeComponent();
        
        this.Width = double.Parse(TomlControl.GetTomlStringList("./settings.toml", "MemoResolution", "Width"));
        this.Height = double.Parse(TomlControl.GetTomlStringList("./settings.toml", "MemoResolution", "Height"));
        
         Panel.Children.Clear();
         MainGrid.Children.Clear();
        Panel.Children.Add(MemoLabel());
        Grid.Background = ApplicationThemeManager.GetAppTheme() == ApplicationTheme.Dark ? Brushes.DimGray : Brushes.LightGray;
    }

    private TextBlock MemoLabel()
    {
        TextBlock text = new TextBlock
        {
            Text = _memo[2],
            VerticalAlignment = VerticalAlignment.Stretch,
            HorizontalAlignment = HorizontalAlignment.Center,
            FontSize = _fontSize
        };
        return text;
    }
    
    private TextBox MemoTextBox()
    {
        TextBox textBox = new TextBox
        {
            Text = _memo[2],
            VerticalAlignment = VerticalAlignment.Stretch,
            HorizontalAlignment = HorizontalAlignment.Center,
            FontSize = _fontSize
        };
        return textBox;
    }

    private void ExitBtn_OnClick(object sender, RoutedEventArgs e)
    {
       this.Close();
    }

    private void MemoWindow_OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
        {
           this.DragMove();
        }
    }

    private void ModeButton_OnClick(object sender, RoutedEventArgs e)
    {
        
    }

    private void SaveButton_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            string[] _ = { _memo[0], _memo[1], ((TextBox)Panel.Children[0]).Text };
            File.WriteAllLines(_filePath, _);
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
            throw;
        }
        this.Close();
    }

    private void ModeButton_OnUnchecked(object sender, RoutedEventArgs e)
    {
        Panel.Children.Clear();
        MainGrid.Children.Clear();
        Panel.Children.Add(MemoLabel());
    }

    private void ModeButton_OnChecked(object sender, RoutedEventArgs e)
    {
           Panel.Children.Clear();
        Panel.Children.Add(MemoTextBox());
        Button saveButton = new Button
        {
            Content = LocalizeControl.GetLocalize<string>("SimpleSave"),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Bottom
        };
        saveButton.Click += SaveButton_OnClick;
        MainGrid.Children.Add(saveButton);
        
    }
}