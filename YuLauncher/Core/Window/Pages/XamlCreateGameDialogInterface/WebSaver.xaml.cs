using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using YuLauncher.Core.lib;

namespace YuLauncher.Core.Window.Pages.XamlCreateGameDialogInterface;

public partial class WebSaver : DialogInterface
{
    public static event EventHandler? OnNameChangeWebSaverSaveClicked;
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
        OnNameChangeWebSaverSaveClicked?.Invoke(this,EventArgs.Empty);
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
    }
}