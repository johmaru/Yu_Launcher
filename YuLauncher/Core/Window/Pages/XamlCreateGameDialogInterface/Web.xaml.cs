using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace YuLauncher.Core.Window.Pages.XamlCreateGameDialogInterface;

public partial class Web : DialogInterface
{
    public static event EventHandler? OnNameChangeWebSaveClicked;
    public Web(string[] data,string name,string path) : base(data,name,path)
    {
        InitializeComponent();
        InterFace.SetNameLabel(NameTextBlock,InterFace.IsDark());
        InterFace.SetNameBox(NameBox,name,InterFace.IsDark());
        
        InterFace.SetNameLabel(UrlTextBlock,InterFace.IsDark());
        InterFace.SetPathBox(UrlBox,NewPath,InterFace.IsDark());
    }

    private async void SaveButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (NameBox.Text != Name)
        {
            string newPath = Path.Replace(Name,NameBox.Text);
            await Task.Run(() => File.Move(Path,newPath));
            
            
        }
        
        if (UrlBox.Text != NewPath)
        {
            string[] lines = await File.ReadAllLinesAsync(Path);
            lines[0] = UrlBox.Text;
            
            await File.WriteAllLinesAsync(Path,lines);
        }

        switch (WebviewSwitch.IsChecked)
        {
            case true:
            {
                string[] lines = await File.ReadAllLinesAsync(Path);
                lines[3] = "true";
            
                await File.WriteAllLinesAsync(Path,lines);
                break;
            }
            case false:
            {
                string[] lines = await File.ReadAllLinesAsync(Path);
                lines[3] = "false";
            
                await File.WriteAllLinesAsync(Path,lines);
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
       string [] nowData = await File.ReadAllLinesAsync(Path);
        if (nowData.Length == 3)
        {
            List <string> nowDataList = new(nowData);
            if (nowDataList == null) throw new ArgumentNullException(nameof(nowDataList));
            
            nowDataList.Add("false");
            await File.WriteAllLinesAsync(Path,nowDataList);
        }
        
        if (nowData[3] == "true")
        {
            WebviewSwitch.IsChecked = true;
        }
        else
        {
            WebviewSwitch.IsChecked = false;
        }
    }
}