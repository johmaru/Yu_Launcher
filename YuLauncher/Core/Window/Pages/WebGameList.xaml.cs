﻿﻿using System;
 using System.Collections.ObjectModel;
 using System.Diagnostics;
 using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using HtmlAgilityPack;
using Wpf.Ui;
using Wpf.Ui.Appearance;
using YuLauncher.Core.lib;
using Button = Wpf.Ui.Controls.Button;
using Image = System.Windows.Controls.Image;
using TextBlock = Wpf.Ui.Controls.TextBlock;

namespace YuLauncher.Core.Window.Pages;

public class NewsItem
{
    public string Title { get; set; }
    public string IconSource { get; set; }
    public SolidColorBrush? TextTheme { get; set; }
    public string Url { get; set; }
}
public partial class WebGameList : Page
{
    private static string[]? _files;
    private readonly GameButton _gameButton = new();
    public ObservableCollection<NewsItem> NewsItems { get; set; }
    private ThemeService _theme = new();
    public WebGameList()
    {
        InitializeComponent();
        GameList.GameControl();
        LoggerController.LogInfo("WebGameList Initialized");
        NewsItems = new ObservableCollection<NewsItem>();
        DataContext = this;
        Task.Run(NewsParserAsync);
        
        PageControlCreate.DeleteFileMenuClicked.Subscribe(_ => GameList_OnFileUpdate(this, EventArgs.Empty));
        CreateGameDialog.CloseObservable.Subscribe(_ => GameList_OnFileUpdate(this, EventArgs.Empty));
        PropertyDialog.AllGameListPanelUpdate.Subscribe( _ => GameList_OnFileUpdate(this, EventArgs.Empty));
        
        SizeChanged += (sender, args) =>
        {
            NewsGrid.Height = WindowHeight * 0.3;
        };
    }
    

    private async Task NewsParserAsync()
    {
        try
        {
            var url = @"https://app.famitsu.com/group/news/page/1";
            HtmlWeb web = new HtmlWeb();
            var doc = await web.LoadFromWebAsync(url);
            var primalNodes = doc.DocumentNode.SelectNodes("//div[@class='PrimaryCardInner_cardText__P7Lcc']/p/a");
            var primalUrlNodes = doc.DocumentNode.SelectNodes("//div[@class='PrimaryCardInner_cardText__P7Lcc']/p/a");
            var primalNodeIcon = doc.DocumentNode.SelectNodes("//div[@class='PrimaryCard_cardMedia__ufh2C  undefined']/a/div/img"); 
            
            var subNodes = doc.DocumentNode.SelectNodes("//div[@class='SecondaryCard_body__FOIoi ']");
            var subUrlNodes = doc.DocumentNode.SelectNodes("//div[@class='SecondaryCard_body__FOIoi ']/p/a");
            var subNodeIcon = doc.DocumentNode.SelectNodes("//div[@class='SecondaryCard_media__KSLGm undefined']/a/div/img");
            var theme = _theme.GetTheme();
            
            
            
                for (int i = 0; i < primalNodes.Count; i++)
                {
                    var title = primalNodes[i].InnerText;
                    var iconSource = primalNodeIcon[i].GetAttributeValue("src", "");
                    var contentUrl = "https://app.famitsu.com/" + primalUrlNodes[i].GetAttributeValue("href", "");
                    var mainAncestor = primalNodes[i].Ancestors("main").FirstOrDefault();

                    if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(iconSource)) continue;

                    if (mainAncestor == null) continue;
                    if (theme == ApplicationTheme.Dark)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            NewsItems.Add(new NewsItem
                            {
                                Title = title,
                                IconSource = iconSource,
                                Url = contentUrl,
                                TextTheme = new SolidColorBrush(Colors.White)
                            });
                        });
                    }
                    else
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            NewsItems.Add(new NewsItem
                            {
                                Title = title,
                                IconSource = iconSource,
                                Url = contentUrl,
                                TextTheme = new SolidColorBrush(Colors.Black)
                            });
                        });
                    }
                }
                
                Console.WriteLine(subNodeIcon.Count);

                for (int i = 0; i < subNodes.Count; i++)
                {
                        var title = subNodes[i].InnerText;
                        var iconSource = subNodeIcon[i].GetAttributeValue("src", "");
                        var contentUrl = "https://app.famitsu.com/" + subUrlNodes[i].GetAttributeValue("href", "");
                        var mainAncestor = subNodes[i].Ancestors("main").FirstOrDefault();
                        
                        if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(iconSource)) continue;

                        if (mainAncestor == null) continue;
                        if (theme == ApplicationTheme.Dark)
                        {
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                NewsItems.Add(new NewsItem
                                {
                                    Title = title,
                                    IconSource = iconSource,
                                    Url = contentUrl,
                                    TextTheme = new SolidColorBrush(Colors.White)
                                });
                            });
                        }
                        else
                        {
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                NewsItems.Add(new NewsItem
                                {
                                    Title = title,
                                    IconSource = iconSource,
                                    Url = contentUrl,
                                    TextTheme = new SolidColorBrush(Colors.Black)
                                });
                            });
                        }
                    
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
        if (_files != null)
            foreach (var file in _files)
            {
                if (Path.GetExtension(file) != ".json") continue;
                JsonControl.ApplicationJsonData data = await JsonControl.ReadExeJson(file);
                try
                {
                    if (data.FileExtension == "WebGame")
                    {
                        WebGameListPanel.Children.Add(_gameButton.GameButtonShow(data.Name, data));
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


    private void WebGameList_OnPreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        DependencyObject? source = e.OriginalSource as DependencyObject;
        while (source != null && !(source is Button))
        {
            source = VisualTreeHelper.GetParent(source);
        }

        if (source is Button button)
        {
            ContextMenu = button.ContextMenu;
        }
        else
        {
            Console.WriteLine("false");
            ContextMenu = PageControlCreate.GameListShowContextMenu(false,new JsonControl.ApplicationJsonData());
        }
    }

    private async void WebGameList_OnLoaded(object sender, RoutedEventArgs e)
    {
        WebGameListPanel.Children.Clear();
        
        await Task.Run(() => _files = Directory.GetFiles(FileControl.Main.Directory));

        if (_files == null) return;
        foreach (var file in _files)
        {
            if (Path.GetExtension(file) != ".json") continue;
            JsonControl.ApplicationJsonData data = await JsonControl.ReadExeJson(file);
            try
            {
                if (data.FileExtension == "WebGame")
                {
                    WebGameListPanel.Children.Add(_gameButton.GameButtonShow(data.Name, data));
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
    
    private void UIElement_OnMouseEnter(object sender, MouseEventArgs e)
    {
        if (sender is System.Windows.Controls.TextBlock text)
        {
            text.Foreground = new SolidColorBrush(Colors.GreenYellow);
        }
    }

    private void UIElement_OnMouseLeave(object sender, MouseEventArgs e)
    {
        if (sender is System.Windows.Controls.TextBlock text)
        {
            text.Foreground = _theme.GetTheme() switch
            {
                ApplicationTheme.Dark => new SolidColorBrush(Colors.White),
                ApplicationTheme.Light => new SolidColorBrush(Colors.Black),
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }

    private void UIElement_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is not System.Windows.Controls.TextBlock text) return;
        if (text.Tag is string url)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
            
        }
    }
}