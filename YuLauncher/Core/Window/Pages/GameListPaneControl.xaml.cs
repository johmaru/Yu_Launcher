using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Threading;
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

public class GameListItem : INotifyPropertyChanged
{
    public string Name { get; set; } = string.Empty;
    public string? FileExtension { get; set; }
    public BitmapSource? IconSource { get; set; }
    public bool IsFileMissing { get; set; }
    public JsonControl.ApplicationJsonData Data { get; set; }
    public bool IsWebGame => Data.FileExtension == "WebGame";
    private string _loginTodayText = "";
    public string LoginTodayText
    {
        get => _loginTodayText;
        set { if (_loginTodayText == value) return; _loginTodayText = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LoginTodayText))); }
    }
    public event PropertyChangedEventHandler? PropertyChanged;
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
    private readonly DispatcherTimer _loginTimer = new() { Interval = TimeSpan.FromSeconds(60) };
    private bool _loginSubscribed;
    private DateTime _loginDate;
    private TimeSpan _loginOffset;
    private int _summaryVersion, _historyVersion, _historyOffset;
    private long? _historyGameId;

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
        _loginTimer.Tick += (_, _) =>
        {
            var now = DateTimeOffset.Now;
            if (now.Date != _loginDate || now.Offset != _loginOffset) _ = RefreshLoginSummariesAsync();
        };
    }

    public static void GameControl()
    {
        if (Directory.Exists(FileControl.Main.Directory)) return;
        Directory.CreateDirectory(FileControl.Main.Directory);
        LoggerController.LogInfo("Create Game Directory");
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (!_loginSubscribed) { WebGameLoginRepository.LoginRecorded += OnLoginRecorded; _loginSubscribed = true; }
        _loginTimer.Start();
        _ = ReloadGamesAsync();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (_loginSubscribed) { WebGameLoginRepository.LoginRecorded -= OnLoginRecorded; _loginSubscribed = false; }
        _loginTimer.Stop();
        ++_summaryVersion; ++_historyVersion; _historyGameId = null;
    }

    private static (DateTimeOffset Start, DateTimeOffset End) LoginDay()
    {
        var date = DateTime.Today;
        return (new DateTimeOffset(date).ToUniversalTime(), new DateTimeOffset(date.AddDays(1)).ToUniversalTime());
    }
    private static string LoginMethod(WebGameLoginMode mode) => LocalizeControl.GetLocalize<string>(mode switch
    {
        WebGameLoginMode.Connection => "WebGameLoginConnection",
        WebGameLoginMode.Url => "WebGameLoginUrl",
        WebGameLoginMode.Element => "WebGameLoginElement",
        WebGameLoginMode.JavaScript => "WebGameLoginJavaScript",
        WebGameLoginMode.Network => "WebGameLoginNetwork",
        _ => throw new ArgumentOutOfRangeException(nameof(mode))
    });
    private static string TodayText(WebGameLoginSummary? summary) => summary?.HasRecordToday == true
        ? string.Format(LocalizeControl.GetLocalize<string>("WebGameLoginRecordedToday"), LoginMethod(summary.TodayMethod!.Value))
        : LocalizeControl.GetLocalize<string>("WebGameLoginNoRecordToday");

    private void OnLoginRecorded(long gameId)
    {
        _ = Dispatcher.InvokeAsync(async () =>
        {
            if (!IsLoaded || !_allGames.Any(g => g.Data.Id == gameId)) return;
            await RefreshLoginSummariesAsync();
            if (_historyGameId == gameId) await LoadLoginHistoryAsync();
        });
    }
    private async Task RefreshLoginSummariesAsync()
    {
        var version = ++_summaryVersion;
        var reloadVersion = _reloadVersion;
        var ids = _allGames.Where(g => g.IsWebGame).Select(g => g.Data.Id).ToArray();
        var day = LoginDay();
        _loginDate = DateTime.Today; _loginOffset = DateTimeOffset.Now.Offset;
        try
        {
            var summaries = await Task.Run(() => WebGameLoginRepository.GetSummaries(ids, day.Start, day.End));
            if (version != _summaryVersion || reloadVersion != _reloadVersion || !IsLoaded) return;
            foreach (var item in _allGames.Where(g => ids.Contains(g.Data.Id)))
                item.LoginTodayText = TodayText(summaries.GetValueOrDefault(item.Data.Id));
        }
        catch (Exception ex)
        {
            if (version != _summaryVersion || reloadVersion != _reloadVersion || !IsLoaded) return;
            foreach (var item in _allGames.Where(g => ids.Contains(g.Data.Id))) item.LoginTodayText = LocalizeControl.GetLocalize<string>("WebGameLoginReadFailed");
            LoggerController.LogError($"WebGame login summary read failed: category={ex.GetType().Name}");
        }
    }

    private async Task LoadLoginHistoryAsync()
    {
        var id = _historyGameId; var offset = _historyOffset; var version = ++_historyVersion;
        if (id is null) return;
        LoginHistoryPreviousButton.IsEnabled = LoginHistoryNextButton.IsEnabled = false;
        LoginHistoryErrorBar.IsOpen = false;
        try
        {
            var result = await Task.Run(() => (Rows: WebGameLoginRepository.GetHistory(id.Value, offset, 51),
                Last: WebGameLoginRepository.GetHistory(id.Value, 0, 1).FirstOrDefault()));
            if (version != _historyVersion || id != _historyGameId || !IsLoaded) return;
            LoginHistoryList.ItemsSource = result.Rows.Take(50).Select(r => new
            { Time = r.DetectedAtUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"), Method = LoginMethod(r.Method) }).ToList();
            LoginHistoryEmptyText.Visibility = result.Rows.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            LoginLastTimeText.Text = result.Last is null ? "" : string.Format(LocalizeControl.GetLocalize<string>("WebGameLoginLastTime"),
                result.Last.DetectedAtUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"));
            LoginHistoryPreviousButton.IsEnabled = offset > 0;
            LoginHistoryNextButton.IsEnabled = result.Rows.Count > 50;
        }
        catch (Exception ex)
        {
            if (version != _historyVersion || id != _historyGameId || !IsLoaded) return;
            LoginHistoryList.ItemsSource = null;
            LoginHistoryEmptyText.Visibility = Visibility.Collapsed;
            LoginLastTimeText.Text = "";
            LoginHistoryErrorBar.IsOpen = true;
            LoggerController.LogError($"WebGame login history read failed: gameId={id}, category={ex.GetType().Name}");
        }
    }
    private async void LoginHistoryPrevious(object sender, RoutedEventArgs e) { _historyOffset = Math.Max(0, _historyOffset - 50); await LoadLoginHistoryAsync(); }
    private async void LoginHistoryNext(object sender, RoutedEventArgs e) { _historyOffset += 50; await LoadLoginHistoryAsync(); }

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
            Dictionary<long, WebGameLoginSummary>? summaries = null;
            var day = LoginDay();
            try { summaries = await Task.Run(() => WebGameLoginRepository.GetSummaries(games.Where(g => g.FileExtension == "WebGame").Select(g => g.Id).ToArray(), day.Start, day.End)); }
            catch (Exception ex) { LoggerController.LogError($"WebGame login summary read failed: category={ex.GetType().Name}"); }
            if (version != _reloadVersion) return;
            _loginDate = DateTime.Today; _loginOffset = DateTimeOffset.Now.Offset;
            var items = new List<GameListItem>();
            var conversionFailed = false;
            foreach (var data in games)
            {
                if (!MatchesFilter(data)) continue;
                try
                {
                    var item = CreateGameListItem(data);
                    if (item.IsWebGame) item.LoginTodayText = summaries is null
                        ? LocalizeControl.GetLocalize<string>("WebGameLoginReadFailed") : TodayText(summaries.GetValueOrDefault(data.Id));
                    items.Add(item);
                }
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
            if (_historyGameId.HasValue) _ = LoadLoginHistoryAsync();
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
        _historyGameId = null; _historyOffset = 0; ++_historyVersion;
        LoginHistoryList.ItemsSource = null; LoginHistorySection.Visibility = Visibility.Collapsed;
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
        var loginId = data.FileExtension == "WebGame" ? (long?)data.Id : null;
        LoginHistorySection.Visibility = loginId.HasValue ? Visibility.Visible : Visibility.Collapsed;
        if (_historyGameId != loginId)
        {
            _historyGameId = loginId; _historyOffset = 0; ++_historyVersion;
            LoginHistoryList.ItemsSource = null; LoginLastTimeText.Text = "";
            LoginHistoryEmptyText.Visibility = Visibility.Collapsed; LoginHistoryErrorBar.IsOpen = false;
            _ = LoadLoginHistoryAsync();
        }
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
