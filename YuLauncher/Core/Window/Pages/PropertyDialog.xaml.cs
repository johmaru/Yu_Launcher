using System;
using System.ComponentModel;
using System.Reactive.Subjects;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;
using YuLauncher.Core.lib;
using YuLauncher.Core.Window.Pages.XamlCreateGameDialogInterface;
using Application = YuLauncher.Core.Window.Pages.XamlCreateGameDialogInterface.Application;
using Web = YuLauncher.Core.Window.Pages.XamlCreateGameDialogInterface.Web;

namespace YuLauncher.Core.Window.Pages;

public partial class PropertyDialog : FluentWindow
{
    
    private static Subject<int> OnAllGameListPanelUpdate = new();
    public static IObservable<int> AllGameListPanelUpdate => OnAllGameListPanelUpdate;
    
    private JsonControl.ApplicationJsonData _data;
    
    private Application? _application;
    private Web? _web;
    private WebGame? _webGame;
    private WebSaver? _webSaver;
    public PropertyDialog(JsonControl.ApplicationJsonData data)
    {
        InitializeComponent();
        Grid.Background = ApplicationThemeManager.GetAppTheme() == ApplicationTheme.Dark ? Brushes.DimGray : Brushes.LightGray;
        
        _data = data;

        switch (data.FileExtension)
        {
            case "exe":
                _application = new Application(data);
                Frame.NavigationService.Navigate(_application);
                break;
            case "WebGame":
                _webGame = new WebGame(data);
                Frame.NavigationService.Navigate(_webGame);
                break;
            case "web":
                _web = new Web(data);
                Frame.NavigationService.Navigate(_web);
                break;
            case "WebSaver":
                _webSaver = new WebSaver(data);
                Frame.NavigationService.Navigate(_webSaver);
                break;
            default:
                this.Close();
            break;
        }

        if (_application != null)
            _application.NameChangeSaveClicked.Subscribe(n => AllOnNameChangeSaveClicked(this, EventArgs.Empty,n));
        else if (_web != null)
            _web.NameChangeSaveClicked.Subscribe(n => AllOnNameChangeSaveClicked(this, EventArgs.Empty,n));
        else if (_webGame != null)
            _webGame.NameChangeSaveClicked.Subscribe(n => AllOnNameChangeSaveClicked(this, EventArgs.Empty,n));
        else if (_webSaver != null)
            _webSaver.NameChangeSaveClicked.Subscribe(n => AllOnNameChangeSaveClicked(this, EventArgs.Empty,n));
    }

    private void AllOnNameChangeSaveClicked(object? sender, EventArgs e,int value)
    {
        if (value == 0)
        {
            OnAllGameListPanelUpdate.OnNext(0);
            this.Close();
        }
        else if (value == 1)
        {
            OnAllGameListPanelUpdate.OnNext(1);
            this.Close();
        }
        else if (value == 2)
        {
            OnAllGameListPanelUpdate.OnNext(2);
            this.Close();
        }
        else if (value == 3)
        {
            OnAllGameListPanelUpdate.OnNext(3);
            this.Close();
        }
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
}