using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;
using YuLauncher.Core.Window.Pages.XamlCreateGameDialogInterface;
using Application = YuLauncher.Core.Window.Pages.XamlCreateGameDialogInterface.Application;
using Web = YuLauncher.Core.Window.Pages.XamlCreateGameDialogInterface.Web;

namespace YuLauncher.Core.Window.Pages;

public partial class PropertyDialog : FluentWindow
{
    public static event EventHandler? OnGameListApplicationPanelUpdate; 
    public static event EventHandler? OnGameListWebPanelUpdate;
    public static event EventHandler? OnGameListWebGamePanelUpdate;
    public PropertyDialog(string[] data,string name,string path)
    {
        InitializeComponent();
        Grid.Background = ApplicationThemeManager.GetAppTheme() == ApplicationTheme.Dark ? Brushes.DimGray : Brushes.LightGray;

        switch (data[1])
        {
            case "exe":
                Frame.NavigationService.Navigate(new Application(data,name,path));
                break;
            case "WebGame":
                Frame.NavigationService.Navigate(new WebGame(data,name,path));
                break;
            case "web":
                Frame.NavigationService.Navigate(new XamlCreateGameDialogInterface.Web(data,name,path));
                break;
            default:
                this.Close();
            break;
        }
        Application.OnNameChangeAppSaveClicked += ApplicationOnOnNameChangeAppSaveClicked;
        XamlCreateGameDialogInterface.Web.OnNameChangeWebSaveClicked += WebOnOnNameChangeWebSaveClicked;
        WebGame.OnNameChangeWebGameSaveClicked += WebGameOnOnNameChangeWebGameSaveClicked;
    }
    private void WebGameOnOnNameChangeWebGameSaveClicked(object? sender, EventArgs e)
    {
        OnGameListWebGamePanelUpdate?.Invoke(this,EventArgs.Empty);
        this.Close();
    }

    private void WebOnOnNameChangeWebSaveClicked(object? sender, EventArgs e)
    {
        OnGameListWebPanelUpdate?.Invoke(this,EventArgs.Empty);
        this.Close();
    }

    private void ApplicationOnOnNameChangeAppSaveClicked(object? sender, EventArgs e)
    {
        OnGameListApplicationPanelUpdate?.Invoke(this,EventArgs.Empty);
        this.Close();
    }

    private void ExitBtn_OnClick(object sender, RoutedEventArgs e)
    {
        this.Close();
    }

    private void PropertyDialog_OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
        {
            DragMove();
        }
           
    }

   protected override void OnClosing(CancelEventArgs e)
   {
       base.OnClosing(e);
       Application.OnNameChangeAppSaveClicked -= ApplicationOnOnNameChangeAppSaveClicked;
         XamlCreateGameDialogInterface.Web.OnNameChangeWebSaveClicked -= WebOnOnNameChangeWebSaveClicked;
         WebGame.OnNameChangeWebGameSaveClicked -= WebGameOnOnNameChangeWebGameSaveClicked;
   }
}