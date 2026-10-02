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
    public BitmapSource? IconSource { get; set; }
    public bool IsFileMissing { get; set; }
    public JsonControl.ApplicationJsonData Data { get; set; }
}

public partial class GameListPaneControl : UserControl
{
    private readonly ObservableCollection<GameListItem> _gameItems = new();
    private List<GameListItem> _allGames = new();
    private int _reloadVersion;
    private bool _isLoading = true;
    private bool _hasLoaded;
    private bool _loadFailed;
    private bool _updatingGenres;
    private bool _updatingItems;

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
        _ = ReloadGamesAsync();
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

    private async Task ReloadGamesAsync()
    {
        var version = ++_reloadVersion;
        LibraryTitleTextBlock.Text = LocalizeControl.GetLocalize<string>(FileExtensionFilter switch
        {
            "WebGame" => "GameListWeb",
            "WebSaver" => "WebSaver",
            _ => "GameList"
        });
        _isLoading = true;
        _loadFailed = false;
        UpdatePresentation();
        try
        {
            var games = await Task.Run(() => GameRepository.GetAll());
            if (version != _reloadVersion) return;
            var items = new List<GameListItem>();
            var conversionFailed = false;
            foreach (var data in games)
            {
                if (!MatchesFilter(data)) continue;
                try { items.Add(CreateGameListItem(data)); }
                catch (Exception ex)
                {
                    conversionFailed = true;
                    LoggerController.LogError($"{ex}");
                }
            }
            if (version != _reloadVersion) return;
            var selectedGenre = GenreComboBox.SelectedItem == GenreAllComboBoxItem
                ? null : (GenreComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString();
            _allGames = items;
            _hasLoaded = true;
            _loadFailed = conversionFailed;
            _updatingGenres = true;
            try
            {
                GenreComboBox.Items.Clear();
                GenreComboBox.Items.Add(GenreAllComboBoxItem);
                ComboBoxItem selected = GenreAllComboBoxItem;
                var genres = new HashSet<string>(StringComparer.Ordinal);
                foreach (var genre in _allGames.SelectMany(item => item.Data.Genre ?? Array.Empty<string>()))
                {
                    if (!genres.Add(genre)) continue;
                    var entry = new ComboBoxItem { Content = genre };
                    GenreComboBox.Items.Add(entry);
                    if (StringComparer.Ordinal.Equals(genre, selectedGenre)) selected = entry;
                }
                GenreComboBox.SelectedItem = selected;
            }
            finally { _updatingGenres = false; }
            ApplyFilters();
        }
        catch (Exception ex)
        {
            if (version != _reloadVersion) return;
            _loadFailed = true;
            LoggerController.LogError($"{ex}");
        }
        finally
        {
            if (version == _reloadVersion)
            {
                _isLoading = false;
                UpdatePresentation();
            }
        }
    }

    private void ApplyFilters()
    {
        var selectedId = (GameListBox.SelectedItem as GameListItem)?.Data.Id;
        var query = SearchBox.Text?.Trim() ?? string.Empty;
        var genre = GenreComboBox.SelectedItem == GenreAllComboBoxItem
            ? null : (GenreComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString();
        _updatingItems = true;
        try
        {
            _gameItems.Clear();
            foreach (var item in _allGames)
            {
                if (genre != null && !(item.Data.Genre?.Contains(genre, StringComparer.Ordinal) ?? false)) continue;
                if (!item.Name.Contains(query, StringComparison.OrdinalIgnoreCase)) continue;
                _gameItems.Add(item);
            }
            GameListBox.SelectedItem = selectedId.HasValue
                ? _gameItems.FirstOrDefault(item => item.Data.Id == selectedId.Value) : null;
        }
        finally { _updatingItems = false; }
        UpdatePresentation();
    }

    private void UpdatePresentation()
    {
        LoadingStatusText.Visibility = _isLoading ? Visibility.Visible : Visibility.Collapsed;
        LoadErrorStatusPanel.Visibility = _loadFailed ? Visibility.Visible : Visibility.Collapsed;
        if (GameListBox.SelectedItem is GameListItem item)
        {
            ShowDetail(item.Data);
            return;
        }
        ShowEmptyState();
        var active = !_hasLoaded && _isLoading ? LibraryLoadingPanel
            : _loadFailed ? LibraryLoadErrorPanel
            : _hasLoaded && _allGames.Count == 0 ? LibraryEmptyPanel
            : _gameItems.Count == 0 ? LibraryNoResultsPanel
            : LibrarySelectPanel;
        LibraryLoadingPanel.Visibility = active == LibraryLoadingPanel ? Visibility.Visible : Visibility.Collapsed;
        LibraryLoadErrorPanel.Visibility = active == LibraryLoadErrorPanel ? Visibility.Visible : Visibility.Collapsed;
        LibraryEmptyPanel.Visibility = active == LibraryEmptyPanel ? Visibility.Visible : Visibility.Collapsed;
        LibraryNoResultsPanel.Visibility = active == LibraryNoResultsPanel ? Visibility.Visible : Visibility.Collapsed;
        LibrarySelectPanel.Visibility = active == LibrarySelectPanel ? Visibility.Visible : Visibility.Collapsed;
    }

    private static GameListItem CreateGameListItem(JsonControl.ApplicationJsonData data)
    {
        BitmapSource? icon = GameButton.GetImage(data);
        return new GameListItem
        {
            Name = data.Name,
            FileExtension = data.FileExtension,
            IconSource = icon,
            IsFileMissing = IsFileMissing(data, icon),
            Data = data
        };
    }

    private static bool IsFileMissing(JsonControl.ApplicationJsonData data, BitmapSource? icon) =>
        data.FileExtension is not ("web" or "WebGame" or "WebSaver")
        && icon is null && !File.Exists(data.FilePath);

    private async void PropertyDialogOnAllGamePanelUpdate(object? sender, EventArgs e, int n)
    {
        if (n is >= 0 and <= 3) await ReloadGamesAsync();
    }

    private async void PropertyDialogPanelUpdate(object? sender, EventArgs e)
    {
        await ReloadGamesAsync();
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

    private void GenreComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_updatingGenres) ApplyFilters();
    }

    private void AddButton_OnClick(object sender, RoutedEventArgs e)
    {
        CreateGameDialog createGameDialog = new CreateGameDialog();
        createGameDialog.Show();
    }

    private async void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F && Keyboard.Modifiers == ModifierKeys.Control)
        {
            e.Handled = true;
            SearchBox.Focus();
            SearchBox.SelectAll();
        }
        else if (e.Key == Key.Escape && Keyboard.Modifiers == ModifierKeys.None && SearchBox.IsKeyboardFocusWithin)
        {
            e.Handled = true;
            SearchBox.Clear();
        }
        else if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.None
                 && GameListBox.IsKeyboardFocusWithin && GameListBox.SelectedItem is GameListItem item)
        {
            e.Handled = true;
            if (!e.IsRepeat) await LaunchAsync(item.Data);
        }
    }

    private void SearchBox_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (!_updatingGenres) ApplyFilters();
    }

    private void GameListBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_updatingItems) UpdatePresentation();
    }

    private void ShowEmptyState()
    {
        EmptyState.Visibility = Visibility.Visible;
        DetailContent.Visibility = Visibility.Collapsed;
        PlayButton.Tag = null;
        PropertyButton.Tag = null;
        MemoButton.Tag = null;
        WikiManageButton.Tag = null;
        GenreManageButton.Tag = null;
    }

    private void ResetFiltersButton_OnClick(object sender, RoutedEventArgs e)
    {
        _updatingGenres = true;
        try
        {
            GenreComboBox.SelectedItem = GenreAllComboBoxItem;
            SearchBox.Clear();
        }
        finally { _updatingGenres = false; }
        ApplyFilters();
        SearchBox.Focus();
    }

    private async void RetryButton_OnClick(object sender, RoutedEventArgs e)
    {
        await ReloadGamesAsync();
    }

    private void ShowDetail(JsonControl.ApplicationJsonData data)
    {
        EmptyState.Visibility = Visibility.Collapsed;
        DetailContent.Visibility = Visibility.Visible;

        BitmapSource? icon = GameButton.GetImage(data);
        HeroImage.Source = icon;
        MissingFileDetailText.Visibility = IsFileMissing(data, icon) ? Visibility.Visible : Visibility.Collapsed;
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
            WikiSection.Visibility = Visibility.Visible;
            WikiDataControl.ItemsSource = data.WikiData.ToList();
        }
        else
        {
            WikiSection.Visibility = Visibility.Collapsed;
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
        if (!GameRepository.ExistsByJsonPath(data.JsonPath)) return;
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
        if (!GameRepository.ExistsByJsonPath(data.JsonPath)) return;
        try
        {
            MemoWindow memoWindow = new MemoWindow(data);
            memoWindow.Closed += async (_, _) => await ReloadGamesAsync();
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
        if (!GameRepository.ExistsByJsonPath(data.JsonPath)) return;
        try
        {
            WikiDataManageWindow wikiWindow = new WikiDataManageWindow(data);
            wikiWindow.Closed += async (_, _) => await ReloadGamesAsync();
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
        if (!GameRepository.ExistsByJsonPath(data.JsonPath)) return;
        try
        {
            GenreManageWindow genreWindow = new GenreManageWindow(data);
            genreWindow.Closed += async (_, _) => await ReloadGamesAsync();
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
