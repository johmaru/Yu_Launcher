using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Wpf.Ui.Controls;
using YuLauncher.Core.lib;

namespace YuLauncher.Core.Window.Pages;

public class GameListItem
{
    public string Name { get; set; } = string.Empty;
    public string? FileExtension { get; set; }
    public BitmapImage? IconSource { get; set; }
    public JsonControl.ApplicationJsonData Data { get; set; }
}

public partial class GameListPaneControl : UserControl
{
    private readonly ObservableCollection<GameListItem> _gameItems = new();
    private List<GameListItem> _allGames = new();

    /// <summary>
    /// フィルタ対象のFileExtension。
    /// "All" = WebGame以外のすべて（GameListと同じ挙動）
    /// "WebGame" = WebGameのみ
    /// "WebSaver" = WebSaverのみ
    /// </summary>
    public string FileExtensionFilter { get; set; } = "All";

    public GameListPaneControl()
    {
        InitializeComponent();
        GameControl();
        GameListBox.ItemsSource = _gameItems;

        PageControlCreate.DeleteFileMenuClicked.Subscribe(_ => PropertyDialogPanelUpdate(this, EventArgs.Empty));
        CreateGameDialog.CloseObservable.Subscribe(_ => PropertyDialogPanelUpdate(this, EventArgs.Empty));
        PropertyDialog.AllGameListPanelUpdate.Subscribe(n => PropertyDialogOnAllGamePanelUpdate(this, EventArgs.Empty, n));
        MainPage.SettingWindowClose.Subscribe(_ => PropertyDialogPanelUpdate(this, EventArgs.Empty));
    }

    public static void GameControl()
    {
        if (Directory.Exists(FileControl.Main.Directory)) return;
        Directory.CreateDirectory(FileControl.Main.Directory);
        LoggerController.LogInfo("Create Game Directory");
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _ = LoadGenre();
    }

    private bool MatchesFilter(JsonControl.ApplicationJsonData data)
    {
        return FileExtensionFilter switch
        {
            "All" => data.FileExtension != "WebGame",
            "WebGame" => data.FileExtension == "WebGame",
            "WebSaver" => data.FileExtension == "WebSaver",
            _ => true
        };
    }

    private async ValueTask LoadGenre()
    {
        string[]? files = null;
        await Task.Run(() => files = Directory.GetFiles(FileControl.Main.Directory));

        List<string> genreList = new();

        if (files != null)
            foreach (var file in files)
            {
                if (Path.GetExtension(file) != ".json") continue;

                var data = await JsonControl.ReadExeJson(file);
                if (!MatchesFilter(data)) continue;

                data.Genre?.ToList().ForEach(x =>
                {
                    if (!genreList.Contains(x))
                    {
                        genreList.Add(x);
                    }
                });
            }

        GenreComboBox.Items.OfType<ComboBoxItem>()
            .Where(x => x != GenreAllComboBoxItem)
            .ToList()
            .ForEach(x => GenreComboBox.Items.Remove(x));

        genreList.ForEach(x =>
        {
            var genre = GenreComboBox.Items.OfType<ComboBoxItem>().FirstOrDefault(item => item.Content?.ToString() == x);
            if (genre != null && (string)genre.Content == x) return;
            ComboBoxItem comboBoxItem = new()
            {
                Content = x
            };
            comboBoxItem.Selected += async (sender, args) =>
            {
                GenreComboBox.SelectedItem = comboBoxItem;
                await LoadGamesByGenre(x);
            };
            GenreComboBox.Items.Add(comboBoxItem);
        });
    }

    private async Task LoadAllGames()
    {
        _allGames.Clear();
        _gameItems.Clear();

        string[]? files = null;
        await Task.Run(() => files = Directory.GetFiles(FileControl.Main.Directory));

        if (files != null)
            foreach (var file in files)
            {
                if (Path.GetExtension(file) != ".json") continue;
                var data = await JsonControl.ReadExeJson(file);
                try
                {
                    if (!MatchesFilter(data)) continue;

                    var item = CreateGameListItem(data);
                    _allGames.Add(item);
                    _gameItems.Add(item);
                }
                catch (Exception ex)
                {
                    LoggerController.LogError($"{ex}");
                    LoggerController.LogError("An I/O error occurred: " + ex.Message);
                }
            }
    }

    private async Task LoadGamesByGenre(string genre)
    {
        _allGames.Clear();
        _gameItems.Clear();

        string[]? files = null;
        await Task.Run(() => files = Directory.GetFiles(FileControl.Main.Directory));

        if (files != null)
            foreach (var file in files)
            {
                if (Path.GetExtension(file) != ".json") continue;
                var data = await JsonControl.ReadExeJson(file);
                try
                {
                    if (MatchesFilter(data) && data.Genre != null && data.Genre.Contains(genre))
                    {
                        var item = CreateGameListItem(data);
                        _allGames.Add(item);
                        _gameItems.Add(item);
                    }
                }
                catch (Exception ex)
                {
                    LoggerController.LogError($"{ex}");
                    LoggerController.LogError("An I/O error occurred: " + ex.Message);
                }
            }
    }

    private static GameListItem CreateGameListItem(JsonControl.ApplicationJsonData data)
    {
        return new GameListItem
        {
            Name = data.Name,
            FileExtension = data.FileExtension,
            IconSource = GameButton.GetImage(data),
            Data = data
        };
    }

    private async void PropertyDialogOnAllGamePanelUpdate(object? sender, EventArgs e, int n)
    {
        switch (n)
        {
            case 0:
            case 1:
                await LoadGenre();
                await LoadAllGames();
                break;
        }
    }

    private async void PropertyDialogPanelUpdate(object? sender, EventArgs e)
    {
        await LoadAllGames();
        Application.Current.MainWindow?.Activate();
    }

    private void OnPreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        DependencyObject? source = e.OriginalSource as DependencyObject;
        while (source != null && source is not ListBoxItem)
        {
            source = VisualTreeHelper.GetParent(source);
        }

        if (source is ListBoxItem listItem && listItem.Tag is JsonControl.ApplicationJsonData data)
        {
            ContextMenu = PageControlCreate.GameListShowContextMenu(true, data);
        }
        else
        {
            ContextMenu = PageControlCreate.GameListShowContextMenu(false, new JsonControl.ApplicationJsonData());
        }
    }

    private async void GenreComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        try
        {
            if (Equals(GenreComboBox.SelectedItem, GenreAllComboBoxItem))
            {
                await LoadAllGames();
            }
        }
        catch (Exception exception)
        {
            LoggerController.LogError($"{exception}");
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

    private void SearchBox_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        var query = SearchBox.Text?.Trim() ?? string.Empty;
        _gameItems.Clear();
        if (string.IsNullOrEmpty(query))
        {
            foreach (var item in _allGames)
                _gameItems.Add(item);
        }
        else
        {
            foreach (var item in _allGames.Where(x => x.Name.Contains(query, StringComparison.OrdinalIgnoreCase)))
                _gameItems.Add(item);
        }
    }

    private void GameListBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (GameListBox.SelectedItem is not GameListItem item)
        {
            ShowEmptyState();
            return;
        }
        ShowDetail(item.Data);
    }

    private void ShowEmptyState()
    {
        EmptyState.Visibility = Visibility.Visible;
        DetailScrollViewer.Visibility = Visibility.Collapsed;
    }

    private void ShowDetail(JsonControl.ApplicationJsonData data)
    {
        EmptyState.Visibility = Visibility.Collapsed;
        DetailScrollViewer.Visibility = Visibility.Visible;

        HeroImage.Source = GameButton.GetImage(data);
        DetailTitleTextBlock.Text = data.Name;
        FileTypeBadgeText.Text = data.FileExtension ?? "unknown";

        GenreChipsControl.ItemsSource = data.Genre?.Length > 0 ? data.Genre : Array.Empty<string>();

        if (!string.IsNullOrEmpty(data.Memo))
        {
            MemoExpander.Visibility = Visibility.Visible;
            MemoTextBlock.Text = data.Memo;
        }
        else
        {
            MemoExpander.Visibility = Visibility.Collapsed;
        }

        if (data.WikiData != null && data.WikiData.Count > 0)
        {
            WikiDataControl.Visibility = Visibility.Visible;
            WikiDataControl.ItemsSource = data.WikiData.ToList();
        }
        else
        {
            WikiDataControl.Visibility = Visibility.Collapsed;
        }

        LaunchOptionsPanel.Children.Clear();

        AddLaunchOption("Log", data.IsUseLog == true ? "ON" : "OFF");
        if (data.FileExtension is "web" or "WebGame" or "WebSaver")
        {
            AddLaunchOption("WebView", data.IsWebView == true ? "ON" : "OFF");
            AddLaunchOption("Mute", data.IsMute ? "ON" : "OFF");
            if (data.Volume.HasValue)
            {
                AddLaunchOption("Volume", $"{data.Volume.Value:P0}");
            }
        }

        PlayButton.Tag = data;
        PropertyButton.Tag = data;
        MemoButton.Tag = data;
        WikiManageButton.Tag = data;
        GenreManageButton.Tag = data;
    }

    private void AddLaunchOption(string label, string value)
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 2) };
        var labelTb = new Wpf.Ui.Controls.TextBlock
        {
            Text = $"{label}: ",
            FontWeight = FontWeights.SemiBold,
            FontSize = 12,
            Opacity = 0.7,
            Width = 100,
            Appearance = Wpf.Ui.Controls.TextColor.Secondary
        };
        var valueTb = new Wpf.Ui.Controls.TextBlock { Text = value, FontSize = 12, Appearance = Wpf.Ui.Controls.TextColor.Primary };
        panel.Children.Add(labelTb);
        panel.Children.Add(valueTb);
        LaunchOptionsPanel.Children.Add(panel);
    }

    private async Task LaunchAsync(JsonControl.ApplicationJsonData data)
    {
        try
        {
            await GameButton.LaunchApplication(data);
            if (data.MultipleLaunch is { Length: > 0 })
            {
                foreach (var multipleLaunch in data.MultipleLaunch)
                {
                    if (string.IsNullOrEmpty(multipleLaunch)) continue;
                    var multipleData = await JsonControl.ReadExeJson($"./Games/{multipleLaunch}.json");
                    await GameButton.LaunchApplication(multipleData);
                }
            }
        }
        catch (Exception ex)
        {
            LoggerController.LogError(ex.Message);
        }
    }

    private async void PlayButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (PlayButton.Tag is not JsonControl.ApplicationJsonData data) return;
        await LaunchAsync(data);
    }

    private async void GameListBox_OnMouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        var element = e.OriginalSource as System.Windows.DependencyObject;
        while (element != null && element is not ListBoxItem)
        {
            element = System.Windows.Media.VisualTreeHelper.GetParent(element);
        }

        if (element is not ListBoxItem item) return;
        if (item.Tag is not JsonControl.ApplicationJsonData data) return;
        await LaunchAsync(data);
    }

    private void PropertyButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (PropertyButton.Tag is not JsonControl.ApplicationJsonData data) return;
        if (!File.Exists(data.JsonPath)) return;
        try
        {
            PropertyDialog propertyDialog = new PropertyDialog(data);
            propertyDialog.Show();
        }
        catch (Exception ex)
        {
            LoggerController.LogError(ex.Message);
        }
    }

    private void MemoButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (MemoButton.Tag is not JsonControl.ApplicationJsonData data) return;
        if (!File.Exists(data.JsonPath)) return;
        try
        {
            MemoWindow memoWindow = new MemoWindow(data);
            memoWindow.Show();
        }
        catch (Exception ex)
        {
            LoggerController.LogError(ex.Message);
        }
    }

    private void WikiManageButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (WikiManageButton.Tag is not JsonControl.ApplicationJsonData data) return;
        if (!File.Exists(data.JsonPath)) return;
        try
        {
            WikiDataManageWindow wikiWindow = new WikiDataManageWindow(data);
            wikiWindow.Show();
        }
        catch (Exception ex)
        {
            LoggerController.LogError(ex.Message);
        }
    }

    private void GenreManageButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (GenreManageButton.Tag is not JsonControl.ApplicationJsonData data) return;
        if (!File.Exists(data.JsonPath)) return;
        try
        {
            GenreManageWindow genreWindow = new GenreManageWindow(data);
            genreWindow.Show();
        }
        catch (Exception ex)
        {
            LoggerController.LogError(ex.Message);
        }
    }

    private void OnDragEnter(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            e.Effects = DragDropEffects.Copy;
        }
        else
        {
            e.Effects = DragDropEffects.None;
        }
        e.Handled = true;
    }
}
