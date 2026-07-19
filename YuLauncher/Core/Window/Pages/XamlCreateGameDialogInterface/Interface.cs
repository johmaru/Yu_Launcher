using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Controls;
using YuLauncher.Core.lib;

namespace YuLauncher.Core.Window.Pages.XamlCreateGameDialogInterface;

public interface IDialogInterface
{
    public JsonControl.ApplicationJsonData Data { get; set; }
}

public abstract class DialogInterface : UserControl, IDialogInterface
{
    public JsonControl.ApplicationJsonData Data { get; set; }
    public string NewPath { get; private set; }

    public string NowName { get; private set; }

    /// <summary>
    /// MultipleLaunch で選択可能な他ゲーム一覧。
    /// 各ページの XAML から ItemsSource でバインドする。
    /// </summary>
    public ObservableCollection<MultipleLaunchCandidate> Candidates { get; } = new();

    protected DialogInterface(JsonControl.ApplicationJsonData data)
    {
        Data = data;
        NewPath = data.FilePath;
        NowName = data.Name;
    }

    /// <summary>
    /// Loaded イベントから呼ぶ。Games フォルダを走査して候補を構築する。
    /// </summary>
    protected async Task LoadMultipleLaunchCandidatesAsync()
    {
        try
        {
            var jsonFiles = Directory.GetFiles("./Games", "*.json");
            var existing = new HashSet<string>(
                Candidates.Select(c => c.Name),
                StringComparer.Ordinal
            );

            foreach (var jf in jsonFiles)
            {
                var candidate = await JsonControl.ReadExeJson(jf);
                if (candidate.Name == Data.Name) continue;
                if (existing.Contains(candidate.Name)) continue;

                Candidates.Add(new MultipleLaunchCandidate(
                    candidate.FilePath,
                    candidate.Name,
                    Data.MultipleLaunch.Contains(candidate.Name)
                ));
                existing.Add(candidate.Name);
            }
        }
        catch (IndexOutOfRangeException ex)
        {
            LoggerController.LogError(ex.Message);
            await ShowOldSystemErrorAsync();
        }
        catch (Exception exception)
        {
            LoggerController.LogError($"{exception}");
        }
    }

    /// <summary>
    /// 候補リストから現在の MultipleLaunch 配列を再構築する。
    /// </summary>
    protected string[] BuildMultipleLaunchFromCandidates()
    {
        return Candidates
            .Where(c => c.IsChecked)
            .Select(c => c.Name)
            .ToArray();
    }

    protected static async Task ShowOldSystemErrorAsync()
    {
        var message = LocalizeControl.GetLocalize<string>("PropertyOldSystemError");
        var title = LocalizeControl.GetLocalize<string>("PropertyCtxHeader");
        var owner = System.Windows.Application.Current.Windows
            .OfType<System.Windows.Window>()
            .FirstOrDefault(w => w.IsActive);
        if (owner != null)
        {
            var dialog = new Wpf.Ui.Controls.MessageBox
            {
                Title = title,
                Content = message,
                Owner = owner
            };
            await dialog.ShowDialogAsync();
        }
        else
        {
            System.Windows.MessageBox.Show(message, title, System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
        }
    }
}

/// <summary>
/// MultipleLaunch 候補の1項目。CheckBox.IsChecked を TwoWay でバインドする。
/// </summary>
public sealed class MultipleLaunchCandidate : INotifyPropertyChanged
{
    private bool _isChecked;

    public string FilePath { get; }
    public string Name { get; }

    public bool IsChecked
    {
        get => _isChecked;
        set
        {
            if (_isChecked == value) return;
            _isChecked = value;
            OnPropertyChanged();
        }
    }

    public MultipleLaunchCandidate(string filePath, string name, bool isChecked)
    {
        FilePath = filePath;
        Name = name;
        _isChecked = isChecked;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
