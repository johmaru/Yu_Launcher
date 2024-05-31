using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Wpf.Ui.Controls;
using YuLauncher.Core.lib;
using Button = Wpf.Ui.Controls.Button;
using MenuItem = Wpf.Ui.Controls.MenuItem;

namespace YuLauncher.Core.Window.Pages;

public partial class GameList : Page
{
    private ManualTomlSettings _tomlControl = new();
    private PageControlCreate _pageControlCreate = new();
    private readonly GameButton _gameButton = new GameButton();
    
    public GameList()
    {
        InitializeComponent();
        MainWindow mainWindow = Application.Current.MainWindow as MainWindow;
        mainWindow.OnBackBtnClick += MainWindow_OnBackBtnClick;
        PageControlCreate.OnDeleteFileMenuClicked += GameList_OnFileUpdate;
        CreateGameDialog.OnClose += GameList_OnFileUpdate;
        GameControl();
        LoggerController.LogInfo("GameList Page Loaded");
    }

    private void GameControl()
    {
        if (!FileControl.ExistGameDirectory(FileControl.Main.Directory))
        {
            Directory.CreateDirectory(FileControl.Main.Directory);
            LoggerController.LogInfo("Create Game Directory");
        }
    }
    
    private void GameList_OnFileUpdate(object sender, EventArgs e)
    {
        Panel.Children.Clear();
        LoggerController.LogInfo("GameList Page Reloaded");
        string[] files = FileControl.GetGameList();
        foreach (var file in files)
        {
            string name = Path.GetFileNameWithoutExtension(file);
            string extension = Path.GetExtension(file);
            string path = File.ReadAllText(file);
            try
            {
                Panel.Children.Add(_gameButton.GameButtonShow(name, path, extension));
            }
            catch (System.IO.IOException ex)
            {
                Console.WriteLine("An I/O error occurred: " + ex.Message);
                throw;
            }
        }
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
           Console.WriteLine("false");
           this.ContextMenu = PageControlCreate.GameListShowContextMenu(false, "");
       }

    }

    private void GameList_OnLoaded(object sender, RoutedEventArgs e)
    {
        string[] files = FileControl.GetGameList();
        foreach (var file in files)
        {
            string name = Path.GetFileNameWithoutExtension(file);
            string extension = Path.GetExtension(file);
            string path = File.ReadAllText(file);
            Panel.Children.Add(_gameButton.GameButtonShow(name, path, extension));
            
            // 拡張子の表示Panelを追加予定
        
            //ファイルのアイコンを取得して表示する予定
        }
    }
    
    private void MainWindow_OnBackBtnClick(object sender, RoutedEventArgs e)
    {
        if (NavigationService != null && NavigationService.CanGoBack)
        {
            NavigationService.GoBack();
        }
    }
}