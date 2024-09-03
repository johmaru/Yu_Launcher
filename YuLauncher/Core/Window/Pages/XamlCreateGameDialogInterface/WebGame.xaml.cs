using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using YuLauncher.Core.lib;

namespace YuLauncher.Core.Window.Pages.XamlCreateGameDialogInterface;

public partial class WebGame : DialogInterface
{
    public static event EventHandler? OnNameChangeWebGameSaveClicked;
    public WebGame(JsonControl.ApplicationJsonData data) : base(data)
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
            await JsonControl.CreateExeJson(Data.JsonPath,Data with { Url = UrlBox.Text });
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
        
        OnNameChangeWebGameSaveClicked?.Invoke(this,EventArgs.Empty);
    }

    private async void WebGame_OnInitialized(object? sender, EventArgs e)
    {
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
                Tag = data.Url
            });
        }
    }
}