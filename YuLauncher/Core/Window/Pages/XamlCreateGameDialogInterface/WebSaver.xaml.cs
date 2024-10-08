using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
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
        foreach (var CB in checkBox)
        {
            if (CB.IsChecked == true)
            {
                if (!data.MultipleLaunch.Contains(CB.Content.ToString()))
                {
                    data = data with { MultipleLaunch = data.MultipleLaunch.Append(CB.Content.ToString()).ToArray() };
                }
                else
                {
                    continue;
                }
            }
            else
            {
                data = data with { MultipleLaunch = data.MultipleLaunch.Where(x => x == (string)CB.Content).ToArray() };
            }

            await JsonControl.CreateExeJson(data.JsonPath, data);
        }
        
        _nameChangeSaveClicked.OnNext(3);
    }

    private async void FrameworkElement_OnInitialized(object? sender, EventArgs e)
    {
        if (Data.IsWebView)
        {
            WebviewSwitch.IsChecked = true;
        }
        else
        {
            WebviewSwitch.IsChecked = false;
        }
        
        var jsonFiles = Directory.GetFiles("./Games", "*.json");
        foreach (var jf in jsonFiles)
        {
            var data = await JsonControl.ReadExeJson(jf);
            if (data.Name == Data.Name)
            {
                continue;
            }

            MultiplePanel.Children.Add(new CheckBox()
            {
                Content = data.Name,
                IsChecked = Data.MultipleLaunch.Contains(data.Name),
                Tag = data.FilePath
            });
        }
    }
}