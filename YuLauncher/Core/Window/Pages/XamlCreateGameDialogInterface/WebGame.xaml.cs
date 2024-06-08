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
       _interFace.SetNameLabel(NameTextBlock,_interFace.IsDark());
       _interFace.SetNameBox(NameBox,name,_interFace.IsDark());
    }

    private async void SaveButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (NameBox.Text != Name)
        {
            string newPath = Path.Replace(Name,NameBox.Text);
            await Task.Run(() => File.Move(Path,newPath));
            
           OnNameChangeWebGameSaveClicked?.Invoke(this,EventArgs.Empty);
        }
    }
}