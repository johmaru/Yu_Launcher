using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Windows;
using YuLauncher.Core.lib;

namespace YuLauncher.Core.Window.Pages.XamlCreateGameDialogInterface;

public partial class Application : DialogInterface
{
    private readonly Subject<int> _nameChangeSaveClicked = new();
    public IObservable<int> NameChangeSaveClicked => _nameChangeSaveClicked.AsObservable();

    public Application(JsonControl.ApplicationJsonData data) : base(data)
    {
        InitializeComponent();
        NameBox.Text = data.Name;
        ExePathNameBox.Text = NewPath;
    }

    private async void Application_OnLoaded(object sender, RoutedEventArgs e)
    {
        ApplicationLogButton.IsChecked = Data.IsUseLog;
        await LoadMultipleLaunchCandidatesAsync();
    }

    private async void SaveButton_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            if (NameBox.Text != NowName)
            {
                Data = Data with { Name = NameBox.Text };
            }

            if (ExePathNameBox.Text != NewPath)
            {
                Data = Data with { FilePath = ExePathNameBox.Text };
            }

            Data = Data with
            {
                IsUseLog = ApplicationLogButton.IsChecked == true,
                MultipleLaunch = BuildMultipleLaunchFromCandidates()
            };

            await JsonControl.CreateExeJson(Data.JsonPath, Data);
            _nameChangeSaveClicked.OnNext(0);
        }
        catch (IndexOutOfRangeException)
        {
            await ShowOldSystemErrorAsync();
        }
        catch (Exception exception)
        {
            LoggerController.LogError($"{exception}");
        }
    }

    private void GenreManageButton_OnClick(object sender, RoutedEventArgs e)
    {
        var genreManageWindow = new GenreManageWindow(Data);
        genreManageWindow.Show();
    }
}
