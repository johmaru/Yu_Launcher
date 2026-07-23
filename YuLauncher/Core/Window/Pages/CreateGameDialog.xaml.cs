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

    public CreateGameDialog()
    {
        InitializeComponent();
        LoggerController.LogInfo("CreateGameDialog Loaded");
    }

    public CreateGameDialog(string path) : this()
    {
        _selectedFilePath = path;
        AppPathLabel.Text = path;
        AppNameBox.Text = Path.GetFileNameWithoutExtension(path);
    }

    private void AppFileButton_OnClick(object sender, RoutedEventArgs e)
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
                AppPathLabel.Text = _selectedFilePath;
                if (string.IsNullOrWhiteSpace(AppNameBox.Text))
                {
                    AppNameBox.Text = Path.GetFileNameWithoutExtension(_selectedFilePath);
                }
                Activate();
            }
            else
            {
                LoggerController.LogWarn("User Cancelled File Selection");
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
        try
        {
            var activeTab = EntryTypeTabControl.SelectedItem as TabItem;
            if (activeTab == ApplicationTab)
            {
                await CreateApplicationAsync(AppNameBox.Text);
            }
            else if (activeTab == WebSiteTab)
            {
                var url = WebSiteUrlBox.Text;
                if (!TryValidateUrl(url)) return;
                await CreateWebEntryAsync(WebSiteNameBox.Text, url, "web", ["WebSite"], withMedia: false);
            }
            else if (activeTab == WebGameTab)
            {
                var url = WebGameUrlBox.Text;
                if (!TryValidateUrl(url)) return;
                await CreateWebEntryAsync(WebGameNameBox.Text, url, "WebGame", ["WebGame"], withMedia: false);
            }
            else if (activeTab == WebSaverTab)
            {
                var url = WebSaverUrlBox.Text;
                if (!TryValidateUrl(url)) return;
                await CreateWebEntryAsync(WebSaverNameBox.Text, url, "WebSaver", ["WebSaver"], withMedia: true);
            }
        }
        catch (Exception exception)
        {
            LoggerController.LogError($"{exception}");
        }
    }

    private async Task CreateApplicationAsync(string name)
    {
        if (string.IsNullOrWhiteSpace(_selectedFilePath))
        {
            ShowError(LocalizeControl.GetLocalize<string>("NotSelectFileError"));
            return;
        }

        var fileExtension = Path.GetExtension(_selectedFilePath)?.TrimStart('.');
        if (string.IsNullOrWhiteSpace(fileExtension))
        {
            ShowError(LocalizeControl.GetLocalize<string>("SelectFileErrorMessage"));
            return;
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            ShowError(LocalizeControl.GetLocalize<string>("NameInput"));
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
            ShowError(LocalizeControl.GetLocalize<string>("NameInput"));
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
            ShowError(exception.Message);
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

        ShowError(LocalizeControl.GetLocalize<string>("NotContainsHttpError"));
        return false;
    }

    /// <summary>
    /// 統一エラー表示。InfoBar でインライン表示し、3秒後に自動消去。
    /// MessageBox は使わない（テーマ・ローカライズの一貫性）。
    /// </summary>
    private void ShowError(string message)
    {
        ErrorInfoBar.Title = LocalizeControl.GetLocalize<string>("CreateGameDialogErrorTitle");
        ErrorInfoBar.Message = message;
        ErrorInfoBar.Severity = InfoBarSeverity.Error;
        ErrorInfoBar.IsOpen = true;

        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
        timer.Tick += (_, _) =>
        {
            ErrorInfoBar.IsOpen = false;
            timer.Stop();
        };
        timer.Start();
    }
}
