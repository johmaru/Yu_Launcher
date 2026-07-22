using System;
using System.Reactive.Subjects;
using System.Windows;
using YuLauncher.Core.lib;

namespace YuLauncher.Core.Window.Pages.XamlCreateGameDialogInterface;

public partial class WebSaver : DialogInterface
{
    private readonly Subject<int> _nameChangeSaveClicked = new();
    public IObservable<int> NameChangeSaveClicked => _nameChangeSaveClicked;

    public WebSaver(JsonControl.ApplicationJsonData data) : base(data)
    {
        InitializeComponent();
        NameBox.Text = data.Name;
        UrlBox.Text = data.Url;
        WindowWidthBox.Value = data.WindowWidth;
        WindowHeightBox.Value = data.WindowHeight;
    }

    private async void WebSaver_OnLoaded(object sender, RoutedEventArgs e)
    {
        WebviewSwitch.IsChecked = Data.IsWebView;
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
                IsWebView = WebviewSwitch.IsChecked,
                WindowWidth = WindowWidthBox.Value,
                WindowHeight = WindowHeightBox.Value,
                MultipleLaunch = BuildMultipleLaunchFromCandidates()
            };

            await JsonControl.CreateExeJson(Data.JsonPath, Data);
            _nameChangeSaveClicked.OnNext(3);
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
