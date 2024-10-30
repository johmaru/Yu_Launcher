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
      
        InitializeComponent();
        
        this._fontSize = double.Parse(TomlControl.GetTomlString("./settings.toml", "MemoFontSize"));
        this.Width = double.Parse(TomlControl.GetTomlString("./settings.toml", "MemoResolution", "Width"));
        this.Height = double.Parse(TomlControl.GetTomlString("./settings.toml", "MemoResolution", "Height"));
        
        Grid.Background = ApplicationThemeManager.GetAppTheme() == ApplicationTheme.Dark ? Brushes.DimGray : Brushes.LightGray;

        _data = data;
        
        CreateMemoContent(false);
    }
    private Task<TextBlock> MemoLabel()
    {
        TextBlock text = new TextBlock
        {
            Text = _data.Memo,
            VerticalAlignment = VerticalAlignment.Stretch,
            HorizontalAlignment = HorizontalAlignment.Center,
            TextWrapping = TextWrapping.WrapWithOverflow,
            FontSize = _fontSize
        };
        return Task.FromResult(text);
    }
    
    private Task<TextBox> MemoTextBox()
    {
        _memoTextBox = new TextBox
        {
            Text = _data.Memo,
            VerticalAlignment = VerticalAlignment.Stretch,
            HorizontalAlignment = HorizontalAlignment.Center,
            FontSize = _fontSize,
            TextWrapping = TextWrapping.WrapWithOverflow,
            Margin = new Thickness(10)
        };
        return Task.FromResult(_memoTextBox);
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

    private Task CreateMemoContent(bool check)
    {
        switch (check)
        {
            case false:
                if (MainGrid.Children.Count > 0) MainGrid.Children.Clear();
                ScrollViewer scrollViewer1 = new ScrollViewer
                {
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch
                };
                StackPanel stackPanel1 = new StackPanel
                {
                    Orientation = Orientation.Vertical,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch
                };
                MainGrid.Children.Add(scrollViewer1);
                scrollViewer1.Content = stackPanel1;
                stackPanel1.Children.Add(MemoLabel().Result);
                break;
            
            case true:
                if (MainGrid.Children.Count > 0) MainGrid.Children.Clear();
                ScrollViewer scrollViewer2 = new ScrollViewer
                {
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch
                };
                StackPanel stackPanel2 = new StackPanel
                {
                    Orientation = Orientation.Vertical,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch
                };
                MainGrid.Children.Add(scrollViewer2);
                scrollViewer2.Content = stackPanel2;
                stackPanel2.Children.Add(MemoTextBox().Result);
                Button saveButton = new Button
                {
                    Content = LocalizeControl.GetLocalize<string>("SimpleSave"),
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Bottom
                };
                saveButton.Click +=async (_,_) =>
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
                stackPanel2.Children.Add(saveButton);
                break;
        }

        return Task.CompletedTask;
    }

    private void ModeButton_OnUnchecked(object sender, RoutedEventArgs e)
    {
        CreateMemoContent(false);
    }

    private void ModeButton_OnChecked(object sender, RoutedEventArgs e)
    {
        CreateMemoContent(true);
    }

    private void MinimizeBtn_OnClick(object sender, RoutedEventArgs e)
    {
        this.WindowState = WindowState.Minimized;
    }
}