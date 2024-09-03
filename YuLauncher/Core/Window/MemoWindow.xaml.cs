using System;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
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
    private readonly double _fontSize;
    private JsonControl.ApplicationJsonData _data;
    private TextBox? _memoTextBox;
    public MemoWindow(JsonControl.ApplicationJsonData data)
    {
       LoadData(data.JsonPath);
        this._fontSize = double.Parse(TomlControl.GetTomlString("./settings.toml", "MemoFontSize"));
        InitializeComponent();
        
        this.Width = double.Parse(TomlControl.GetTomlStringList("./settings.toml", "MemoResolution", "Width"));
        this.Height = double.Parse(TomlControl.GetTomlStringList("./settings.toml", "MemoResolution", "Height"));
        
        Grid.Background = ApplicationThemeManager.GetAppTheme() == ApplicationTheme.Dark ? Brushes.DimGray : Brushes.LightGray;
    }

    private async void LoadData(string jsonPath)
    {
        _data = await JsonControl.ReadExeJson(jsonPath);
    }

    private TextBlock MemoLabel()
    {
        TextBlock text = new TextBlock
        {
            Text = _data.Memo,
            VerticalAlignment = VerticalAlignment.Stretch,
            HorizontalAlignment = HorizontalAlignment.Center,
            FontSize = _fontSize
        };
        return text;
    }
    
    private TextBox MemoTextBox()
    {
        _memoTextBox = new TextBox
        {
            Text = _data.Memo,
            VerticalAlignment = VerticalAlignment.Stretch,
            HorizontalAlignment = HorizontalAlignment.Center,
            FontSize = _fontSize,
            Margin = new Thickness(10)
        };
        return _memoTextBox;
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

    private void ModeButton_OnUnchecked(object sender, RoutedEventArgs e)
    {
        MainGrid.Children.Clear();
        ScrollViewer scrollViewer = new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };
        StackPanel stackPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };
        MainGrid.Children.Add(scrollViewer);
        scrollViewer.Content = stackPanel;
        stackPanel.Children.Add(MemoLabel());
    }

    private void ModeButton_OnChecked(object sender, RoutedEventArgs e)
    {
        MainGrid.Children.Clear();
        ScrollViewer scrollViewer = new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };
        StackPanel stackPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };
        MainGrid.Children.Add(scrollViewer);
        scrollViewer.Content = stackPanel;
        stackPanel.Children.Add(MemoTextBox());
        Button saveButton = new Button
        {
            Content = LocalizeControl.GetLocalize<string>("SimpleSave"),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Bottom
        };
        saveButton.Click +=async (sender, args) =>
        {
            try
            {
                if (_memoTextBox != null)
                {
                  _data = _data with{Memo = _memoTextBox.Text};
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                throw;
            }
            await JsonControl.CreateExeJson(_data.JsonPath,_data);
            this.Close();
        };
       stackPanel.Children.Add(saveButton);
    }

    private void MinimizeBtn_OnClick(object sender, RoutedEventArgs e)
    {
        this.WindowState = WindowState.Minimized;
    }
}