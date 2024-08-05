using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace YuLauncher.Core.Window.Pages.XamlCreateGameDialogInterface;

public partial class Application : DialogInterface
{
    public static event EventHandler? OnNameChangeAppSaveClicked;
    public Application(string[] data,string name,string path) : base(data,name,path)
    {
        InitializeComponent();
        
        InterFace.SetNameLabel(NameTextBlock,InterFace.IsDark());
        InterFace.SetNameBox(NameBox,name,InterFace.IsDark());
        
        InterFace.SetNameLabel(ExePathNameTextBlock,InterFace.IsDark());
        InterFace.SetPathBox(ExePathNameBox,NewPath,InterFace.IsDark());
    }

    private async void SaveButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (NameBox.Text != Name)
        {
                string newPath = Path.Replace(Name,NameBox.Text);
                await Task.Run(() => File.Move(Path,newPath));
        }

        if (ExePathNameBox.Text != NewPath)
        {
           string[] lines = await File.ReadAllLinesAsync(Path);
           lines[0] = ExePathNameBox.Text;
           
           await File.WriteAllLinesAsync(Path,lines);
        }
        string[] lines2 = await File.ReadAllLinesAsync(Path);
        lines2[4] = ApplicationLogButton.IsChecked == true ? "true" : "false";
        await File.WriteAllLinesAsync(Path,lines2);
        
        OnNameChangeAppSaveClicked?.Invoke(this,EventArgs.Empty);
    }

    private async void ApplicationLog_OnInitialized(object? sender, EventArgs e)
    {
        string[] lines = await File.ReadAllLinesAsync(Path);
        if (lines[4] == "true")
        {
            ApplicationLogButton.IsChecked = true;
        }
        else
        {
            ApplicationLogButton.IsChecked = false;
        }
    }
}