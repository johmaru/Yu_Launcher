using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Wpf.Ui;
using Wpf.Ui.Appearance;
using YuLauncher.Core.lib;
using TextBox = Wpf.Ui.Controls.TextBox;

namespace YuLauncher.Core.Window.Pages.XamlCreateGameDialogInterface;

public interface IDialogInterface
{
    string Name { get; set; }
    string[] Data { get; set; }
}

public partial class InterFaceClass
{
    public TextBlock SetNameLabel(TextBlock text,bool isDark)
    {
        text.FontSize = 15;
        text.FontWeight = FontWeights.Bold;
        text.Foreground = isDark ? Brushes.White: Brushes.Black ;
        return text;
    }
    public TextBox SetNameBox(TextBox textBox,string path,bool isDark)
    {
        textBox.Text = path;
        textBox.FontSize = 15;
        textBox.Foreground = isDark ? Brushes.White: Brushes.Black ;
        return textBox;
    }
    
    public TextBox SetPathBox(TextBox textBox,string path,bool isDark)
    {
        textBox.Text = path;
        textBox.FontSize = 15;
        textBox.HorizontalAlignment = HorizontalAlignment.Stretch;
        
        textBox.Foreground = isDark ? Brushes.White: Brushes.Black ;
        return textBox;
    }

    public bool IsDark()
    {
        ThemeService themeService = new();
        if (themeService.GetTheme() == ApplicationTheme.Dark)
        {
            return true;
        }
        return false;
    }
}

public abstract class DialogInterface : UserControl, IDialogInterface
{
    protected new string Name { get;private set; }

    protected string Path { get; private set; }
    public string[] Data { get; set; }
    protected string NewPath { get; private set; }
    
    protected readonly InterFaceClass InterFace = new();
    protected readonly string[] Files = FileControl.GetGameList(); 
    protected DialogInterface(string[] data,string name,string path)
    {
        Data = data;
        NewPath = data[0];
        Name = name;
        Path = path;
    }
    
}