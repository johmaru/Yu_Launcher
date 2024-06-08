using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using YuLauncher.Core.lib;
using Button = Wpf.Ui.Controls.Button;
using Image = System.Windows.Controls.Image;

namespace YuLauncher.Core.Window.Pages;

public partial class WebGameList : Page
{
    private static string[]? _files;
    private readonly GameButton _gameButton = new GameButton();
    public WebGameList()
    {
        InitializeComponent();
        MainWindow? mainWindow = Application.Current.MainWindow as MainWindow;
        if (mainWindow != null) mainWindow.OnBackBtnClick += MainWindow_OnBackBtnClick;
        PageControlCreate.OnDeleteFileMenuClicked += GameList_OnFileUpdate;
        CreateGameDialog.OnClose += GameList_OnFileUpdate;
        GameList.GameControl();
        LoggerController.LogInfo("WebGameList Initialized");
        PropertyDialog.OnGameListWebGamePanelUpdate += GameList_OnFileUpdate;
    }
    
    

    private async void GameList_OnFileUpdate(object? sender, EventArgs e)
    {
        _files = null;
        
        await Task.Run(() => _files = Directory.GetFiles(FileControl.Main.Directory));
        
        WebGameListPanel.Children.Clear();
        IconPanel.Children.Clear();
        foreach (var file in _files)
        {
            string name = Path.GetFileNameWithoutExtension(file);
            string[] path = await File.ReadAllLinesAsync(file);
            try
            {
                if (path[1] == "WebGame")
                {
                    WebGameListPanel.Children.Add(_gameButton.GameButtonShow(name, path, path[1]));
                    IconPanel.Children.Add(new Image()
                    {
                        Source = new BitmapImage(new Uri("https://www.google.com/s2/favicons?domain=" + path[0])),
                        Height = ObjectProperty.GameListObjectHeight,
                        VerticalAlignment = VerticalAlignment.Center,
                        HorizontalAlignment = HorizontalAlignment.Center,
                    });
                }
                else
                {
                    continue;
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                throw;
            }
        }
        LoggerController.LogInfo("WebGameList Reloaded");
    }

    

    private void MainWindow_OnBackBtnClick(object sender, RoutedEventArgs e)
    {
        if (NavigationService is {CanGoBack: true})
        {
            NavigationService.GoBack();
        }
        {
            NavigationService?.Navigate(new MainPage());
        }
    }

    private void WebGameList_OnPreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
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
            Console.WriteLine("false");
            this.ContextMenu = PageControlCreate.GameListShowContextMenu(false, "", Array.Empty<string>(), "");
        }
    }

    private async void WebGameList_OnLoaded(object sender, RoutedEventArgs e)
    {
        WebGameListPanel.Children.Clear();
        IconPanel.Children.Clear();
        
        await Task.Run(() => _files = Directory.GetFiles(FileControl.Main.Directory));
        
        foreach (var file in _files)
        {
            string name = Path.GetFileNameWithoutExtension(file);
            string[] path = await File.ReadAllLinesAsync(file);
            try
            {
                if (path[1] == "WebGame")
                {
                    WebGameListPanel.Children.Add(_gameButton.GameButtonShow(name, path, path[1]));
                    IconPanel.Children.Add(new Image()
                    {
                        Source = new BitmapImage(new Uri("https://www.google.com/s2/favicons?domain=" + path[0])),
                        Height = ObjectProperty.GameListObjectHeight,
                        VerticalAlignment = VerticalAlignment.Center,
                        HorizontalAlignment = HorizontalAlignment.Center,
                    });
                }
                else
                {
                    continue;
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                throw;
            }
        }
    }

    private void AddButton_OnClick(object sender, RoutedEventArgs e)
    {
        CreateGameDialog createGameDialog = new CreateGameDialog();
        createGameDialog.Show();
    }
}