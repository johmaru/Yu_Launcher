using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Wpf.Ui.Controls;
using YuLauncher.Core.lib;
using Button = Wpf.Ui.Controls.Button;
using TextBox = Wpf.Ui.Controls.TextBox;

namespace YuLauncher.Core.Window;

public partial class MemoWindow : FluentWindow
{
    private readonly string[] _memo;
    private readonly string _filePath;
    public MemoWindow(string filePath,string[] memo)
    {
        this._memo = memo;
        this._filePath = filePath;
        InitializeComponent();
         Panel.Children.Clear();
         MainGrid.Children.Clear();
        Panel.Children.Add(MemoLabel());
    }

    private Label MemoLabel()
    {
        Label label = new Label
        {
            Content = _memo[2],
            VerticalAlignment = VerticalAlignment.Stretch,
            HorizontalAlignment = HorizontalAlignment.Center,
            FontSize = 20
        };
        return label;
    }
    
    private TextBox MemoTextBox()
    {
        TextBox textBox = new TextBox
        {
            Text = _memo[2],
            VerticalAlignment = VerticalAlignment.Stretch,
            HorizontalAlignment = HorizontalAlignment.Center,
            FontSize = 20
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