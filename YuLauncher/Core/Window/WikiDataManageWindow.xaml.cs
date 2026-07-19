using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using Wpf.Ui.Controls;
using YuLauncher.Core.lib;

namespace YuLauncher.Core.Window;

public partial class WikiDataManageWindow : FluentWindow
{
    private JsonControl.ApplicationJsonData _data;
    public ObservableCollection<WikiEntry> WikiEntries { get; } = new();

    public WikiDataManageWindow(JsonControl.ApplicationJsonData data)
    {
        InitializeComponent();
        _data = data;
        LoadEntries();
    }

    private void LoadEntries()
    {
        WikiEntries.Clear();
        if (_data.WikiData == null) return;
        foreach (var (key, value) in _data.WikiData)
        {
            WikiEntries.Add(new WikiEntry(key, value));
        }
    }

    private void AddButton_OnClick(object sender, RoutedEventArgs e)
    {
        WikiEntries.Add(new WikiEntry(string.Empty, string.Empty));
    }

    private void DeleteEntry_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Wpf.Ui.Controls.Button btn) return;
        if (btn.Tag is not WikiEntry entry) return;
        WikiEntries.Remove(entry);
    }

    private async void SaveButton_OnClick(object sender, RoutedEventArgs e)
    {
        var distinct = new Dictionary<string, string>();
        foreach (var entry in WikiEntries)
        {
            if (string.IsNullOrEmpty(entry.Key)) continue;
            try
            {
                distinct.Add(entry.Key, entry.Value);
            }
            catch (ArgumentException)
            {
                ShowError(LocalizeControl.GetLocalize<string>("SimpleDuplicateKey"));
                return;
            }
        }

        try
        {
            _data = _data with { WikiData = distinct };
            await JsonControl.CreateExeJson(_data.JsonPath, _data);
            LoadEntries();
            ShowInfo(LocalizeControl.GetLocalize<string>("SimpleCompleted"));
        }
        catch (Exception ex)
        {
            LoggerController.LogError($"{ex}");
            ShowError(ex.Message);
        }
    }

    private static void ShowError(string message)
    {
        var title = LocalizeControl.GetLocalize<string>("WikiDataManageWindowTitle");
        System.Windows.MessageBox.Show(message, title, System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
    }

    private static void ShowInfo(string message)
    {
        var title = LocalizeControl.GetLocalize<string>("WikiDataManageWindowTitle");
        System.Windows.MessageBox.Show(message, title, System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
    }
}

public sealed class WikiEntry : INotifyPropertyChanged
{
    private string _key;
    private string _value;

    public string Key
    {
        get => _key;
        set
        {
            if (_key == value) return;
            _key = value;
            OnPropertyChanged();
        }
    }

    public string Value
    {
        get => _value;
        set
        {
            if (_value == value) return;
            _value = value;
            OnPropertyChanged();
        }
    }

    public WikiEntry(string key, string value)
    {
        _key = key;
        _value = value;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
