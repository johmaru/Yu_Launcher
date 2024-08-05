using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Wpf.Ui.Controls;
using YuLauncher.Core.lib;
using Button = Wpf.Ui.Controls.Button;
using Image = System.Windows.Controls.Image;
using MenuItem = Wpf.Ui.Controls.MenuItem;

namespace YuLauncher.Core.Window.Pages;

public partial class GameList : Page
{
    private static GameList _instance;
    private ManualTomlSettings _tomlControl = new();
    private PageControlCreate _pageControlCreate = new();
    private readonly GameButton _gameButton = new GameButton();
    private static string[]? _files;
    
    
    public GameList()
    {
        InitializeComponent();
        MainWindow mainWindow = Application.Current.MainWindow as MainWindow;
        mainWindow.OnBackBtnClick += MainWindow_OnBackBtnClick;
        PageControlCreate.OnDeleteFileMenuClicked += PropertyDialogPanelUpdate;
        CreateGameDialog.OnClose += PropertyDialogPanelUpdate;
        GameControl();
        LoggerController.LogInfo("GameList Page Loaded");
        PropertyDialog.OnGameListApplicationPanelUpdate += PropertyDialogOnOnGameListApplicationPanelUpdate;
        PropertyDialog.OnGameListWebPanelUpdate += PropertyDialogOnOnGameListWebPanelUpdate;
    }

    private async void PropertyDialogOnOnGameListWebPanelUpdate(object? sender, EventArgs e)
    {
        _files = null;
        
        await Task.Run(() => _files = Directory.GetFiles(FileControl.Main.Directory));
        
        GenreAllUpdate();
    }

    private async void PropertyDialogOnOnGameListApplicationPanelUpdate(object? sender, EventArgs e)
    {
        _files = null;
        
        await Task.Run(() => _files = Directory.GetFiles(FileControl.Main.Directory));
        
        GenreAllUpdate();
    }
    
    private async void PropertyDialogPanelUpdate(object? sender, EventArgs e)
    {
        _files = null;
        
        await Task.Run(() => _files = Directory.GetFiles(FileControl.Main.Directory));
        
        GenreAllUpdate();
    }

    private async void Initialize()
    {
        Panel.Children.Clear();
        ExtensionLabel.Children.Clear();
        IconPanel.Children.Clear();
        
      await Task.Run(() => _files = Directory.GetFiles(FileControl.Main.Directory));
      
      Console.WriteLine("UpdateFiles");
      
      GenreAllUpdate();
    }

    public static void GameControl()
    {
        if (!FileControl.ExistGameDirectory(FileControl.Main.Directory))
        {
            Directory.CreateDirectory(FileControl.Main.Directory);
            LoggerController.LogInfo("Create Game Directory");
        }
    }

    private async void GenreAllUpdate()
    {
        GameList_OnFileUpdate(this, EventArgs.Empty);
        foreach (var file in _files)
        {
            string name = Path.GetFileNameWithoutExtension(file);
            string[] path = await File.ReadAllLinesAsync(file);
            try
            {
                if (path[1] == "WebGame")
                {
                    continue;
                }
                Panel.Children.Add(_gameButton.GameButtonShow(name, path, path[1]));
                ExtensionLabel.Children.Add(new Label
                {
                    Content = LocalizeControl.GetLocalize<string>("ExtensionLabel") + path[1],
                    Height = ObjectProperty.GameListObjectHeight,
                    VerticalAlignment = VerticalAlignment.Stretch,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalContentAlignment = VerticalAlignment.Stretch,
                    HorizontalContentAlignment = HorizontalAlignment.Stretch,
                });
                if (path[1] != "web")
                {
                    if (File.Exists(path[0]))
                    {
                        using (MemoryStream s = new MemoryStream())
                        {
                            Icon? icon =  System.Drawing.Icon.ExtractAssociatedIcon(path[0]);
                            icon?.Save(s);
                            s.Position = 0;
                            BitmapFrame bitmapFrame = BitmapFrame.Create(s, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
                            IconPanel.Children.Add(new Wpf.Ui.Controls.Image()
                            {
                                Source = bitmapFrame,
                                Height = ObjectProperty.GameListObjectHeight,
                                VerticalAlignment = VerticalAlignment.Center,
                                HorizontalAlignment = HorizontalAlignment.Center,
                            });
                    
                        }
                    }
                    else
                    {
                        BitmapImage bitmap = new BitmapImage();
                        
                        bitmap.BeginInit();
                        
                        bitmap.UriSource = new Uri("/image/404-error-3060993_640.png",UriKind.Relative);
                        
                        bitmap.EndInit();
                        
                        IconPanel.Children.Add(new Image()
                        {
                            Source = bitmap,
                            Height = ObjectProperty.GameListObjectHeight,
                            VerticalAlignment = VerticalAlignment.Center,
                            HorizontalAlignment = HorizontalAlignment.Center,
                        });
                    }
                    
                }
                else
                {
                    IconPanel.Children.Add(new Image()
                    {
                        Source = new BitmapImage(new Uri("https://www.google.com/s2/favicons?domain=" + path[0])),
                        Height = ObjectProperty.GameListObjectHeight,
                        VerticalAlignment = VerticalAlignment.Center,
                        HorizontalAlignment = HorizontalAlignment.Center,
                    });
                
                }
            }
            catch (System.IO.IOException ex)
            {
                Console.WriteLine("An I/O error occurred: " + ex.Message);
                LoggerController.LogError("An I/O error occurred: " + ex.Message);
                throw;
            }
            LoggerController.LogInfo($"FileUpdate {name} Extension: {path[1]}");
        }
    }

    private async void GenreExeUpdate()
    {
        GameList_OnFileUpdate(this, EventArgs.Empty);
        Console.WriteLine("GenreExeUpdate");
         foreach (var file in _files)
         {
             string name = Path.GetFileNameWithoutExtension(file);
             string[] path = await File.ReadAllLinesAsync(file);
            
             try
             {
                 if (path[1] == "exe")
                 {
                     Panel.Children.Add(_gameButton.GameButtonShow(name, path, path[1]));
                     ExtensionLabel.Children.Add(new Label
                     {
                         Content = LocalizeControl.GetLocalize<string>("ExtensionLabel") + path[1],
                         Height = ObjectProperty.GameListObjectHeight,
                         VerticalAlignment = VerticalAlignment.Stretch,
                         HorizontalAlignment = HorizontalAlignment.Stretch,
                         VerticalContentAlignment = VerticalAlignment.Stretch,
                         HorizontalContentAlignment = HorizontalAlignment.Stretch,
                     });

                     if (File.Exists(path[0]))
                     {
                         using (MemoryStream s = new MemoryStream())
                         {
                             Icon? icon =  Icon.ExtractAssociatedIcon(path[0]);
                             icon?.Save(s);
                             s.Position = 0;
                             BitmapFrame bitmapFrame = BitmapFrame.Create(s, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
                             IconPanel.Children.Add(new Wpf.Ui.Controls.Image()
                             {
                                 Source = bitmapFrame,
                                 Height = ObjectProperty.GameListObjectHeight,
                                 VerticalAlignment = VerticalAlignment.Center,
                                 HorizontalAlignment = HorizontalAlignment.Center,
                             });
                    
                         }
                     }
                     else
                     {
                         BitmapImage bitmap = new BitmapImage();
                        
                         bitmap.BeginInit();
                        
                         bitmap.UriSource = new Uri("/image/404-error-3060993_640.png",UriKind.Relative);
                        
                         bitmap.EndInit();
                        
                         IconPanel.Children.Add(new Image()
                         {
                             Source = bitmap,
                             Height = ObjectProperty.GameListObjectHeight,
                             VerticalAlignment = VerticalAlignment.Center,
                             HorizontalAlignment = HorizontalAlignment.Center,
                         });
                     }
                 }
                 else
                 {
                     continue;
                 }
             }
             catch (Exception ex)
             {
                 Console.WriteLine(ex);
                 LoggerController.LogError("An I/O error occurred: " + ex.Message);
                 throw;
             }
             LoggerController.LogInfo($"FileUpdate {name} Extension: {path[1]}");
         }
    }

    private async void GenreWebUpdate()
    {
        GameList_OnFileUpdate(this, EventArgs.Empty);
        foreach (var file in _files)
        {
            string name = Path.GetFileNameWithoutExtension(file);
            string[] path = await File.ReadAllLinesAsync(file);
            
            try
            {
                if (path[1] == "web")
                {
                    Panel.Children.Add(_gameButton.GameButtonShow(name, path, path[1]));
                    ExtensionLabel.Children.Add(new Label
                    {
                        Content = LocalizeControl.GetLocalize<string>("ExtensionLabel") + path[1],
                        Height = ObjectProperty.GameListObjectHeight,
                        VerticalAlignment = VerticalAlignment.Stretch,
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                        VerticalContentAlignment = VerticalAlignment.Stretch,
                        HorizontalContentAlignment = HorizontalAlignment.Stretch,
                    });
                
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
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                LoggerController.LogError("An I/O error occurred: " + ex.Message);
                throw;
            }
            LoggerController.LogInfo($"FileUpdate {name} Extension: {path[1]}");
        }
    }
    
    private async void GameList_OnFileUpdate(object sender, EventArgs e)
    {
        Panel.Children.Clear();
        ExtensionLabel.Children.Clear();
        IconPanel.Children.Clear();
        LoggerController.LogInfo("GameList Page Reloaded");
    }

    private void GameList_OnPreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
       /*CreateGameDialog createGameDialog = new CreateGameDialog();
       createGameDialog.Show();*/

       DependencyObject source = e.OriginalSource as DependencyObject;
       while (source != null && !(source is Button))
       {
           source = VisualTreeHelper.GetParent(source);
       }

       if (source is Button button)
       {
           this.ContextMenu = button.ContextMenu;
       }
       else
       {
           this.ContextMenu = PageControlCreate.GameListShowContextMenu(false, "",new string[]{""},"");
       }

    }
    
    private void MainWindow_OnBackBtnClick(object sender, RoutedEventArgs e)
    {
        if (NavigationService is { CanGoBack: true })
        {
            NavigationService.GoBack();
        }
    }

    private void GenreComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        try
        {
            if (Equals(GenreComboBox.SelectedItem, GenreAllComboBoxItem))
            {
                Initialize();
            }
            else if (Equals(GenreComboBox.SelectedItem, GenreExeComboBoxItem))
            {
               GenreExeUpdate();
            }
            else if (Equals(GenreComboBox.SelectedItem, GenreWebsiteComboBoxItem))
            {
               GenreWebUpdate();
            }
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
            throw;
        }
    }

    private void GenreComboBox_OnLoaded(object sender, RoutedEventArgs e)
    {
        GenreComboBox.SelectedItem = GenreAllComboBoxItem;
    }

    private void AddButton_OnClick(object sender, RoutedEventArgs e)
    {
      CreateGameDialog createGameDialog = new CreateGameDialog();
      createGameDialog.Show();
    }
}