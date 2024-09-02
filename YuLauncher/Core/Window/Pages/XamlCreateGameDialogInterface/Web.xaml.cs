using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using YuLauncher.Core.lib;

namespace YuLauncher.Core.Window.Pages.XamlCreateGameDialogInterface;

public partial class Web : DialogInterface
{
    public static event EventHandler? OnNameChangeWebSaveClicked;
    public Web(JsonControl.ApplicationJsonData data) : base(data)
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

        switch (WebviewSwitch.IsChecked)
        {
            case true:
            {
                Data = Data with { IsWebView = true };
               await JsonControl.CreateExeJson(Data.JsonPath,Data);
                break;
            }
            case false:
            {
                Data = Data with { IsWebView = false };
                await JsonControl.CreateExeJson(Data.JsonPath,Data);
                break;
            }
            default:
                MessageBox.Show("Error");
                break;
        }
        
        OnNameChangeWebSaveClicked?.Invoke(this,EventArgs.Empty);
    }

    private async void FrameworkElement_OnInitialized(object? sender, EventArgs e)
    {
       try
       {

           if (Data.IsWebView == true)
           {
               WebviewSwitch.IsChecked = true;
           }
           else
           {
               WebviewSwitch.IsChecked = false;
           }
       }
       catch (IndexOutOfRangeException ex)
       {
           MessageBox.Show("this file is old system on file and you using old version of file");
           LoggerController.LogError(ex.Message);
       }
       catch (Exception exception)
       {
           Console.WriteLine(exception);
           throw;
       }
    }
}