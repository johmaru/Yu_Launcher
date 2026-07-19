using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using Wpf.Ui.Controls;
using YuLauncher.Core.lib;

namespace YuLauncher.Core.Window;

public partial class GenreManageWindow : FluentWindow
{
    private JsonControl.ApplicationJsonData _data;
    public ObservableCollection<GenreItem> Genres { get; } = new();

    public GenreManageWindow(JsonControl.ApplicationJsonData data)
    {
        InitializeComponent();
        _data = data;
        LoadGenres();
    }

    private void LoadGenres()
    {
        Genres.Clear();
        foreach (var genre in _data.Genre)
        {
            Genres.Add(new GenreItem(genre, isChecked: true, OnItemChanged));
        }
    }

    private async Task SaveGenresAsync()
    {
        var newGenres = Genres.Where(g => g.IsChecked).Select(g => g.Name).ToArray();
        _data = _data with { Genre = newGenres };
        await JsonControl.CreateExeJson(_data.JsonPath, _data);
    }

    private async void OnItemChanged()
    {
        try
        {
            await SaveGenresAsync();
        }
        catch (Exception ex)
        {
            LoggerController.LogError($"{ex}");
        }
    }

    private async void AddButton_OnClick(object sender, RoutedEventArgs e)
    {
        var name = GenreText.Text?.Trim();
        if (string.IsNullOrEmpty(name))
        {
            ShowWarning(LocalizeControl.GetLocalize<string>("GenreNameInput"));
            return;
        }

        if (Genres.Any(g => string.Equals(g.Name, name, StringComparison.Ordinal)))
        {
            ShowWarning(LocalizeControl.GetLocalize<string>("SimpleDuplicateKey"));
            return;
        }

        var item = new GenreItem(name, isChecked: true, OnItemChanged);
        Genres.Add(item);
        await SaveGenresAsync();

        GenreText.Text = string.Empty;
        ShowInfo(LocalizeControl.GetLocalize<string>("GenreAdd"));
    }

    private static void ShowWarning(string message)
    {
        var title = LocalizeControl.GetLocalize<string>("GenreManageWindowTitle");
        System.Windows.MessageBox.Show(message, title, System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
    }

    private static void ShowInfo(string message)
    {
        var title = LocalizeControl.GetLocalize<string>("GenreManageWindowTitle");
        System.Windows.MessageBox.Show(message, title, System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
    }
}

public sealed class GenreItem : INotifyPropertyChanged
{
    private bool _isChecked;
    public string Name { get; }
    private readonly Action _onChange;

    public bool IsChecked
    {
        get => _isChecked;
        set
        {
            if (_isChecked == value) return;
            _isChecked = value;
            OnPropertyChanged();
            _onChange?.Invoke();
        }
    }

    public GenreItem(string name, bool isChecked, Action onChange)
    {
        Name = name;
        _isChecked = isChecked;
        _onChange = onChange;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
