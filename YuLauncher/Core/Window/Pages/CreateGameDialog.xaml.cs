using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using HtmlAgilityPack;
using Microsoft.Win32;
using UtfUnknown;
using Wpf.Ui.Controls;
using YuLauncher.Core.lib;

namespace YuLauncher.Core.Window.Pages;

public partial class CreateGameDialog : FluentWindow
{
    private static readonly Subject<int> _onClose = new();
    public static IObservable<int> CloseObservable => _onClose.AsObservable();

    private string _selectedFilePath = "";
    private int _openNum;

    public CreateGameDialog()
    {
        InitializeComponent();
        LoggerController.LogInfo("CreateGameDialog Loaded");
    }

    public CreateGameDialog(string path) : this()
    {
        _selectedFilePath = path;
        PathLabel.Text = path;
        NameBox.Text = Path.GetFileNameWithoutExtension(path);
    }

    private void UrlButton_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var ofd = new OpenFileDialog
            {
                Title = LocalizeControl.GetLocalize<string>("SelectFileLabel"),
                RestoreDirectory = true
            };

            if (ofd.ShowDialog() == true)
            {
                _selectedFilePath = ofd.FileName;
                PathLabel.Text = _selectedFilePath;
                if (string.IsNullOrWhiteSpace(NameBox.Text))
                {
                    NameBox.Text = Path.GetFileNameWithoutExtension(_selectedFilePath);
                }
                Activate();
            }
            else
            {
                LoggerController.LogWarn("User Cancelled File Selection");
                ShowErrorBriefly();
                Activate();
            }
        }
        catch (Exception exception)
        {
            LoggerController.LogError($"{exception}");
        }
    }

    private async void CreateButton_OnClick(object sender, RoutedEventArgs e)
    {
        var selectedItem = GenreSelectComboBox.SelectedItem;
        var name = NameBox.Text;
        var url = UrlBlock.Text;

        try
        {
            switch (selectedItem)
            {
                case ComboBoxItem item when item == GenreApplicationComboBoxItem:
                    await CreateApplicationAsync(name);
                    break;
                case ComboBoxItem item when item == GenreWebSiteComboBoxItem:
                    if (!TryValidateUrl(url)) return;
                    await CreateWebEntryAsync(name, url, "web", ["WebSite"], withMedia: false);
                    break;
                case ComboBoxItem item when item == GenreWebGameComboBoxItem:
                    if (!TryValidateUrl(url)) return;
                    await CreateWebEntryAsync(name, url, "WebGame", ["WebGame"], withMedia: false);
                    break;
                case ComboBoxItem item when item == GenreWebSaverComboBoxItem:
                    if (!TryValidateUrl(url)) return;
                    await CreateWebEntryAsync(name, url, "WebSaver", ["WebSaver"], withMedia: true);
                    break;
                default:
                    await ShowErrorDialogAsync(LocalizeControl.GetLocalize<string>("SelectGenreError"));
                    break;
            }
        }
        catch (Exception exception)
        {
            LoggerController.LogError($"{exception}");
        }
    }

    private async Task CreateApplicationAsync(string name)
    {
        var fileExtension = Path.GetExtension(_selectedFilePath)?.TrimStart('.');
        if (string.IsNullOrWhiteSpace(fileExtension))
        {
            await ShowErrorDialogAsync(LocalizeControl.GetLocalize<string>("SelectFileErrorMessage"));
            return;
        }

        if (string.IsNullOrWhiteSpace(_selectedFilePath))
        {
            await ShowErrorDialogAsync(LocalizeControl.GetLocalize<string>("NotSelectFileError"));
            return;
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            await ShowErrorDialogAsync(LocalizeControl.GetLocalize<string>("NameInput"));
            return;
        }

        var data = new JsonControl.ApplicationJsonData
        {
            FilePath = _selectedFilePath,
            FileExtension = fileExtension,
            Url = "",
            Name = name,
            JsonPath = $"{FileControl.Main.Directory}\\{name}.json",
            Memo = "",
            IsWebView = false,
            IsUseLog = false,
            MultipleLaunch = [""],
            Genre = ["Application"]
        };
        await JsonControl.CreateExeJson(data.JsonPath, data);

        _onClose.OnNext(0);
        Close();
    }

    private async Task CreateWebEntryAsync(string name, string url, string fileExtension, string[] genre, bool withMedia)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            await ShowErrorDialogAsync(LocalizeControl.GetLocalize<string>("NameInput"));
            return;
        }

        string? htmlPath = null;
        if (withMedia)
        {
            htmlPath = await DownloadSiteAssetsAsync(name, url);
            if (htmlPath == null)
            {
                // すでにエラー表示済み
                return;
            }
        }

        var data = new JsonControl.ApplicationJsonData
        {
            FilePath = htmlPath ?? "",
            JsonPath = $"{FileControl.Main.Directory}\\{name}.json",
            FileExtension = fileExtension,
            Name = name,
            Url = url,
            Memo = "",
            IsWebView = false,
            IsUseLog = false,
            IsMute = false,
            Volume = 1.0,
            MultipleLaunch = [""],
            Genre = genre
        };
        await JsonControl.CreateExeJson(data.JsonPath, data);

        _onClose.OnNext(0);
        Close();
    }

    private async Task<string?> DownloadSiteAssetsAsync(string name, string url)
    {
        try
        {
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string htmlPath = Path.Combine(baseDirectory, "html");
            string contentDir = Path.Combine(htmlPath, name);
            string completePath = Path.GetFullPath(contentDir);
            Directory.CreateDirectory(completePath);

            using var handler = new HttpClientHandler
            {
                UseCookies = true,
                CookieContainer = new CookieContainer()
            };
            using var client = new HttpClient(handler);

            using var response = await client.GetAsync(url);
            var contentBytes = await response.Content.ReadAsByteArrayAsync();
            var detection = CharsetDetector.DetectFromBytes(contentBytes);
            var encoding = detection.Detected.Encoding;
            var content = encoding.GetString(contentBytes);

            var doc = new HtmlDocument();
            doc.LoadHtml(content);

            var imageNodes = doc.DocumentNode.SelectNodes("//img[@src]");
            if (imageNodes != null)
            {
                foreach (var img in imageNodes)
                {
                    try
                    {
                        var imageUrl = img.GetAttributeValue("src", "");
                        if (!Uri.IsWellFormedUriString(imageUrl, UriKind.Absolute))
                        {
                            imageUrl = new Uri(new Uri(url), imageUrl).AbsoluteUri;
                        }
                        var imgBytes = await client.GetByteArrayAsync(imageUrl);
                        var imgFileName = Path.GetFileName(new Uri(imageUrl).LocalPath);
                        await File.WriteAllBytesAsync(Path.Combine(completePath, imgFileName), imgBytes);
                        img.SetAttributeValue("src", imgFileName);
                    }
                    catch (NotSupportedException)
                    {
                        // 画像取得失敗は無視
                    }
                    catch (Exception ex)
                    {
                        LoggerController.LogError($"{ex}");
                    }
                }
            }

            bool isShtml = url.EndsWith(".shtml", StringComparison.OrdinalIgnoreCase);
            string fileName = isShtml ? $"{name}.shtml" : $"{name}.html";
            string fullPath = Path.Combine(completePath, fileName);

            await using (var writer = new StreamWriter(fullPath, false, encoding))
            {
                await writer.WriteLineAsync(content);
            }

            return fullPath;
        }
        catch (Exception exception)
        {
            LoggerController.LogError(exception.Message);
            await ShowErrorDialogAsync(exception.Message);
            return null;
        }
    }

    private bool TryValidateUrl(string url)
    {
        if (Uri.TryCreate(url, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
        {
            return true;
        }

        _ = ShowErrorDialogAsync(LocalizeControl.GetLocalize<string>("NotContainsHttpError"));
        return false;
    }

    private Task ShowErrorDialogAsync(string message)
    {
        var title = LocalizeControl.GetLocalize<string>("PropertyCtxHeader");
        System.Windows.MessageBox.Show(message, title, System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        return Task.CompletedTask;
    }

    private void ShowErrorBriefly()
    {
        ErrLabel.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Red);
        ErrLabel.Visibility = Visibility.Visible;
        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
        timer.Tick += (_, _) =>
        {
            ErrLabel.Visibility = Visibility.Collapsed;
            timer.Stop();
        };
        timer.Start();
    }
    private void GenreSelectComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_openNum == 0) return;

        var selected = GenreSelectComboBox.SelectedItem as ComboBoxItem;
        var isWeb = selected == GenreWebSiteComboBoxItem
                 || selected == GenreWebGameComboBoxItem
                 || selected == GenreWebSaverComboBoxItem;
        var isApp = selected == GenreApplicationComboBoxItem;

        UrlButton.Visibility = isApp ? Visibility.Visible : Visibility.Collapsed;
        PathLabel.Visibility = isApp ? Visibility.Visible : Visibility.Collapsed;
        UrlBlock.Visibility = isWeb ? Visibility.Visible : Visibility.Collapsed;

        if (!isApp && !isWeb)
        {
            UrlButton.Visibility = Visibility.Collapsed;
            PathLabel.Visibility = Visibility.Collapsed;
            UrlBlock.Visibility = Visibility.Collapsed;
        }
    }

    private void GenreSelectComboBox_OnLoaded(object sender, RoutedEventArgs e)
    {
        GenreSelectComboBox.SelectedIndex = 0;
        LoggerController.LogInfo("GenreSelectComboBox Loaded");

        GenreLabel.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Yellow);
        GenreLabel.Visibility = Visibility.Visible;
        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
        timer.Tick += (_, _) =>
        {
            GenreLabel.Visibility = Visibility.Collapsed;
            timer.Stop();
        };
        timer.Start();

        _openNum += 1;
    }
}
