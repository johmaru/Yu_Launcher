using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reactive.Subjects;
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
    private readonly GameButton _gameButton = new GameButton();
    private static string[]? _files;
    
    public GameList()
    {
        InitializeComponent();
        GameControl();
        LoggerController.LogInfo("GameList Page Loaded");
        
        PageControlCreate.DeleteFileMenuClicked.Subscribe(_ => PropertyDialogPanelUpdate(this, EventArgs.Empty));
        CreateGameDialog.CloseObservable.Subscribe(_ => PropertyDialogPanelUpdate(this, EventArgs.Empty));
        PropertyDialog.AllGameListPanelUpdate.Subscribe( n => PropertyDialogOnAllGamePanelUpdate(this, EventArgs.Empty,n));
        MainPage.SettingWindowClose.Subscribe(_ => PropertyDialogPanelUpdate(this, EventArgs.Empty));
    }
    
    private async void PropertyDialogOnAllGamePanelUpdate(object? sender, EventArgs e, int n)
    {
        switch (n)
        {
            case 0:
                // App Update
                _files = null;
                if (!Panel.Children.Count.Equals(0)) Panel.Children.Clear();
        
                await Task.Run(() => _files = Directory.GetFiles(FileControl.Main.Directory));
        
                GenreAllUpdate();
                break;
            case 1:
                // Web Update
                _files = null;
                if (!Panel.Children.Count.Equals(0)) Panel.Children.Clear();
        
                await Task.Run(() => _files = Directory.GetFiles(FileControl.Main.Directory));
        
                GenreAllUpdate();
                break;
        }
        
    }
    
    private async void PropertyDialogPanelUpdate(object? sender, EventArgs e)
    {
        _files = null;
        if (!Panel.Children.Count.Equals(0)) Panel.Children.Clear();
        
        await Task.Run(() => _files = Directory.GetFiles(FileControl.Main.Directory));
        
        GenreAllUpdate();

        Application.Current.MainWindow?.Activate();
    }

    private async void Initialize()
    {
        Panel.Children.Clear();
        
      await Task.Run(() => _files = Directory.GetFiles(FileControl.Main.Directory));
      
      Console.WriteLine("UpdateFiles");
      
      GenreAllUpdate();
    }

    public static void GameControl()
    {
        if (Directory.Exists(FileControl.Main.Directory)) return;
        Directory.CreateDirectory(FileControl.Main.Directory);
        LoggerController.LogInfo("Create Game Directory");
    }

    private async void GenreAllUpdate()
    {
        GameList_OnFileUpdate(this, EventArgs.Empty);
        if (_files != null)
            foreach (var file in _files)
            {
                string name = Path.GetFileNameWithoutExtension(file);

                if (Path.GetExtension(file) == ".txt")
                {
                   LoggerController.LogError($"{file}: is not a valid file");
                }
                else
                {
                    var jsonData = await JsonControl.ReadExeJson(file);
                    try
                    {
                        if (jsonData.FileExtension == "WebGame")
                        {
                            continue;
                        }

                        Panel.Children.Add(_gameButton.GameButtonShow(jsonData.Name, jsonData));
                    }
                    catch (IOException ex)
                    {
                        Console.WriteLine("An I/O error occurred: " + ex.Message);
                        LoggerController.LogError("An I/O error occurred: " + ex.Message);
                        throw;
                    }

                    LoggerController.LogInfo($"FileUpdate {name} Extension: {jsonData.FileExtension}");
                }
            }

        Viewer.Content = null;
        Viewer.Content = Panel;
    }

    private async void GenreExeUpdate()
    {
        GameList_OnFileUpdate(this, EventArgs.Empty);
        Console.WriteLine("GenreExeUpdate");
        if (_files != null)
            foreach (var file in _files)
            {
                string name = Path.GetFileNameWithoutExtension(file);
                var data = await JsonControl.ReadExeJson(file);

                try
                {
                    if (data.FileExtension == "exe")
                    {
                        Panel.Children.Add(_gameButton.GameButtonShow(data.Name, data));
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

                LoggerController.LogInfo($"FileUpdate {name} Extension: {data.FileExtension}");
            }

        Viewer.Content = null;
         Viewer.Content = Panel;
    }

    private async void GenreWebUpdate()
    {
        GameList_OnFileUpdate(this, EventArgs.Empty);
        if (_files != null)
            foreach (var file in _files)
            {
                string name = Path.GetFileNameWithoutExtension(file);
                var data = await JsonControl.ReadExeJson(file);

                try
                {
                    if (data.FileExtension == "web")
                    {
                        Panel.Children.Add(_gameButton.GameButtonShow(data.Name, data));
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

                LoggerController.LogInfo($"FileUpdate {name} Extension: {data.FileExtension}");
            }

        Viewer.Content = null;
        Viewer.Content = Panel;
    }
    
    private void GameList_OnFileUpdate(object sender, EventArgs e)
    {
        Panel.Children.Clear();
        LoggerController.LogInfo("GameList Page Reloaded");
    }

    private void GameList_OnPreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
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
           ContextMenu = PageControlCreate.GameListShowContextMenu(false,new JsonControl.ApplicationJsonData());
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
    
    private void Main_OnDragEnter(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            string[]? files = (string[])e.Data.GetData(DataFormats.FileDrop) ?? throw new InvalidOperationException();
            if (files.Length > 0)
            {
                string fileExtension = Path.GetExtension(files[0]);
                if (fileExtension == ".exe")
                {
                    e.Effects = DragDropEffects.Copy;
                }
                else
                {
                    e.Effects = DragDropEffects.None;
                }
              
            }
        }
        else
        {
            e.Effects = DragDropEffects.None;
        }
    }
}

