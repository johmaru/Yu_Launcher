using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace YuLauncher.Core.Window.Pages.XamlCreateGameDialogInterface;

public partial class WebGame : DialogInterface
{
    public static event EventHandler? OnNameChangeWebGameSaveClicked;
    public WebGame(string[] data,string name,string path) : base(data,name,path)
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
        
        OnNameChangeWebGameSaveClicked?.Invoke(this,EventArgs.Empty);
    }
}