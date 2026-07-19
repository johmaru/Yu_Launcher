using System;
using System.Reactive.Subjects;
using System.Windows;
using YuLauncher.Core.lib;

namespace YuLauncher.Core.Window.Pages.XamlCreateGameDialogInterface;

public partial class WebGame : DialogInterface
{
    private readonly Subject<int> _nameChangeSaveClicked = new();
    public IObservable<int> NameChangeSaveClicked => _nameChangeSaveClicked;

    public WebGame(JsonControl.ApplicationJsonData data) : base(data)
    {
        InitializeComponent();
        NameBox.Text = data.Name;
        UrlBox.Text = data.Url;
    }

    private async void WebGame_OnLoaded(object sender, RoutedEventArgs e)
    {
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

            if (UrlBox.Text != NewPath)
            {
                Data = Data with { Url = UrlBox.Text };
            }

            Data = Data with
            {
                MultipleLaunch = BuildMultipleLaunchFromCandidates()
            };

            await JsonControl.CreateExeJson(Data.JsonPath, Data);
            _nameChangeSaveClicked.OnNext(2);
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
