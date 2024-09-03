using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using YuLauncher.Core.lib;
using Button = Wpf.Ui.Controls.Button;

namespace YuLauncher.Core.Window.Pages;

public partial class WebSaverList : Page
{
    private static string[]? _files;
    private readonly GameButton _gameButton = new GameButton();
    public WebSaverList()
    {
        InitializeComponent();
        MainWindow? mainWindow = Application.Current.MainWindow as MainWindow;
        mainWindow.OnBackBtnClick += MainWindow_OnBackBtnClick;
        PageControlCreate.OnDeleteFileMenuClicked += PageControlCreateOnOnDeleteFileMenuClicked;
        CreateGameDialog.OnClose += PropertyDialogOnOnGameListWebSaverPanelUpdate;
        PropertyDialog.OnGameListWebSaverPanelUpdate += PropertyDialogOnOnGameListWebSaverPanelUpdate;
        Initialize();
    }

    private async void PageControlCreateOnOnDeleteFileMenuClicked(object? sender, EventArgs e)
    {
        _files = null;
        await Task.Run(() => _files = Directory.GetFiles(FileControl.Main.Directory));
        SaverListLoad();
    }

    private async void PropertyDialogOnOnGameListWebSaverPanelUpdate(object? sender, EventArgs e)
    {
        _files = null;
        await Task.Run(() => _files = Directory.GetFiles(FileControl.Main.Directory));
        SaverListLoad();
    }

    private async void SaverListLoad()
    {
        Panel.Children.Clear();
        foreach (var file in _files)
        {
            string name = Path.GetFileNameWithoutExtension(file);
            JsonControl.ApplicationJsonData data = await JsonControl.ReadExeJson(file);
            try
            {
                if (data.FileExtension == "WebSaver")
                {
                    Panel.Children.Add(_gameButton.GameButtonShow(data.Name,data));
                }
                else
                {
                  continue;
                }
            }
            catch (Exception e)
            {
                LoggerController.LogError(e.Message);
            }
        }
    }

    private async void Initialize()
    {
        Panel.Children.Clear();
        
        await Task.Run(() => _files = Directory.GetFiles(FileControl.Main.Directory));
        SaverListLoad();
    }

    private void MainWindow_OnBackBtnClick(object sender, RoutedEventArgs e)
    {
        if (NavigationService is { CanGoBack: true })
        {
            NavigationService.GoBack();
        }
    }

    private void AddButton_OnClick(object sender, RoutedEventArgs e)
    {
        CreateGameDialog createGameDialog = new CreateGameDialog();
        createGameDialog.Show();
    }

    private void WebSaverList_OnPreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        DependencyObject source = e.OriginalSource as DependencyObject;
        while (source != null && !(source is Wpf.Ui.Controls.Button))
        {
            source = VisualTreeHelper.GetParent(source);
        }

        if (source is Button button)
        {
            this.ContextMenu = button.ContextMenu;
        }
        else
        {
            this.ContextMenu = PageControlCreate.GameListShowContextMenu(false,new JsonControl.ApplicationJsonData());
        }
    }
}