using System;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using HtmlAgilityPack;
using Wpf.Ui;
using Wpf.Ui.Appearance;
using YuLauncher.Core.lib;
using Button = Wpf.Ui.Controls.Button;
using Image = System.Windows.Controls.Image;
using TextBlock = Wpf.Ui.Controls.TextBlock;

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
        Task.Run(() => NewsParserAsync());
    }
    

    private async Task NewsParserAsync()
    {
        try
        {
            var url = @"https://app.famitsu.com/category/news/";
            HtmlWeb web = new HtmlWeb();
            var doc = await web.LoadFromWebAsync(url);
            var nodes = doc.DocumentNode.SelectNodes("//h2[@class='article-title']/a[@title]");
            var nodeIcon = doc.DocumentNode.SelectNodes("//div[@class='article-img']/a/img[contains(@src, '.jpg') or contains(@src, '.png')]");
            ThemeService themeService = new();
            var theme = themeService.GetTheme();

            foreach (var node in nodes)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (theme == ApplicationTheme.Dark)
                    {
                        NewsPanel.Children.Add(new TextBlock()
                        {
                            Text = node.InnerText,
                            Foreground = System.Windows.Media.Brushes.White,
                            HorizontalAlignment= HorizontalAlignment.Stretch,
                            VerticalAlignment= VerticalAlignment.Stretch,
                            Margin = new Thickness(0, 0, 20, 0)
                        });
                    }
                    else
                    {
                        NewsPanel.Children.Add(new TextBlock()
                        {
                            Text = node.InnerText,
                            Foreground = System.Windows.Media.Brushes.Black,
                            HorizontalAlignment= HorizontalAlignment.Stretch,
                            VerticalAlignment= VerticalAlignment.Stretch,
                            Margin = new Thickness(0, 0, 20, 0)
                        });
                    }
                });
            }

            foreach (var node in nodeIcon)
            {
                var nodeValue = node.Attributes["src"].Value;
                await Application.Current.Dispatcher.Invoke(async () =>
                {
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(nodeValue, UriKind.Absolute);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.DownloadCompleted += (sender, args) =>
                    {
                    };
                    bitmap.EndInit();
                    
                    NewsIcon.Children.Add(new Image()
                    {
                        Source = bitmap,
                        VerticalAlignment = VerticalAlignment.Stretch,
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                        Margin = new Thickness(0, 0, 20, 0)
                    });
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
        }
    }

    private async void GameList_OnFileUpdate(object? sender, EventArgs e)
    {
        _files = null;
        
        await Task.Run(() => _files = Directory.GetFiles(FileControl.Main.Directory));
        
        WebGameListPanel.Children.Clear();
        foreach (var file in _files)
        {
            string name = Path.GetFileNameWithoutExtension(file);
            string[] path = await File.ReadAllLinesAsync(file);
            try
            {
                if (path[1] == "WebGame")
                {
                    WebGameListPanel.Children.Add(_gameButton.GameButtonShow(name,path, path[1]));
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
        
        await Task.Run(() => _files = Directory.GetFiles(FileControl.Main.Directory));
        
        foreach (var file in _files)
        {
            string name = Path.GetFileNameWithoutExtension(file);
            string[] path = await File.ReadAllLinesAsync(file);
            try
            {
                if (path[1] == "WebGame")
                {
                    WebGameListPanel.Children.Add(_gameButton.GameButtonShow(name,path, path[1]));
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