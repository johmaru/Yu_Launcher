using System;
using System.Windows;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;
using YuLauncher.Core.lib;
using Button = Wpf.Ui.Controls.Button;

namespace YuLauncher.Core.Window;

public partial class MemoWindow : FluentWindow
{
    private readonly double _fontSize;
    private JsonControl.ApplicationJsonData _data;

    public MemoWindow(JsonControl.ApplicationJsonData data)
    {
        InitializeComponent();

        _fontSize = double.Parse(TomlControl.GetTomlString("./settings.toml", "MemoFontSize"));
        Width = double.Parse(TomlControl.GetTomlString("./settings.toml", "MemoResolution", "Width"));
        Height = double.Parse(TomlControl.GetTomlString("./settings.toml", "MemoResolution", "Height"));

        _data = data;

        MemoTextBlock.Text = data.Memo;
        MemoTextBlock.FontSize = _fontSize;

        MemoTextBox.Text = data.Memo;
        MemoTextBox.FontSize = _fontSize;
    }

    private void ModeButton_OnChecked(object sender, RoutedEventArgs e)
    {
        ViewScrollViewer.Visibility = Visibility.Collapsed;
        EditPanel.Visibility = Visibility.Visible;
        MemoTextBox.Text = _data.Memo;
        MemoTextBox.Focus();
    }

    private void ModeButton_OnUnchecked(object sender, RoutedEventArgs e)
    {
        ApplyViewMode();
    }

    private void ApplyViewMode()
    {
        EditPanel.Visibility = Visibility.Collapsed;
        ViewScrollViewer.Visibility = Visibility.Visible;
        MemoTextBlock.Text = _data.Memo;
    }

    private async void SaveButton_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            _data = _data with { Memo = MemoTextBox.Text };
            await JsonControl.CreateExeJson(_data.JsonPath, _data);
            MemoTextBlock.Text = _data.Memo;
            ApplyViewMode();
            ModeButton.IsChecked = false;
        }
        catch (Exception exception)
        {
            LoggerController.LogError($"{exception}");
        }
    }
}
