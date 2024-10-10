using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using YuLauncher.Core.lib;

namespace YuLauncher.Core.Window.Pages.XamlCreateGameDialogInterface;

public partial class WebSaver : DialogInterface
{
    private Subject<int> _nameChangeSaveClicked = new();
    public IObservable<int> NameChangeSaveClicked => _nameChangeSaveClicked;
    public WebSaver(JsonControl.ApplicationJsonData data) : base(data)
    {
        InitializeComponent();
        InterFace.SetNameLabel(NameTextBlock,InterFace.IsDark());
        InterFace.SetNameBox(NameBox,data.Name,InterFace.IsDark());
        
        InterFace.SetNameLabel(UrlTextBlock,InterFace.IsDark());
        InterFace.SetPathBox(UrlBox,data.Url,InterFace.IsDark());
    }

    private async void SaveButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (NameBox.Text != NowName)
        {
            Data = Data with { Name = NameBox.Text };
            await JsonControl.CreateExeJson(Data.JsonPath,Data);
        }
        
        if (UrlBox.Text != NewPath)
        {
            Data = Data with { Url = UrlBox.Text };
            await JsonControl.CreateExeJson(Data.JsonPath,Data);
        }
        
        var data = await JsonControl.ReadExeJson(Data.JsonPath);
        var checkBox = MultiplePanel.Children.OfType<CheckBox>();
        foreach (var cb in checkBox)
        {
            if (cb.IsChecked == true)
            {
                if (!data.MultipleLaunch.Contains(cb.Content.ToString()))
                {
                    data = data with { MultipleLaunch = data.MultipleLaunch.Append(cb.Content.ToString()).ToArray() };
                }
                else
                {
                    continue;
                }
            }
            else
            {
                data = data with { MultipleLaunch = data.MultipleLaunch.Where(x => x == (string)cb.Content).ToArray() };
            }

            await JsonControl.CreateExeJson(data.JsonPath, data);
        }
        
        _nameChangeSaveClicked.OnNext(3);
    }

    private async void FrameworkElement_OnInitialized(object? sender, EventArgs e)
    {
        WebviewSwitch.IsChecked = Data.IsWebView;
        
        var jsonFiles = Directory.GetFiles("./Games", "*.json");
        foreach (var jf in jsonFiles)
        {
            var data = await JsonControl.ReadExeJson(jf);
            if (data.Name == Data.Name)
            {
                continue;
            }

            TextBlock text = new TextBlock()
            {
                Text = data.Name,
                FontSize = 20
            };

            var checkBox = new CheckBox()
            {
                Content = text,
                IsChecked = Data.MultipleLaunch.Contains(data.Name),
                Tag = data.FilePath,
            };

            checkBox.SetBinding(WidthProperty, new Binding("ActualWidth")
            {
                Source = this,
                Mode = BindingMode.OneWay
            });

            MultiplePanel.Children.Add(checkBox);
        }
    }

    private void WebSaver_OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        UrlBox.Width = e.NewSize.Width - UrlTextBlock.ActualWidth - 50;
        NameBox.Width = e.NewSize.Width - NameTextBlock.ActualWidth - 50;
    }
}