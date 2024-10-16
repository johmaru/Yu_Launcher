using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using HtmlAgilityPack;
using Microsoft.Web.WebView2.Core;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using Wpf.Ui.Controls;
using YuLauncher.Core.lib;
using MessageBox = System.Windows.MessageBox;
using MessageBoxButton = System.Windows.MessageBoxButton;
using Timer = System.Timers.Timer;

namespace YuLauncher.Core.Window.Pages;

public partial class CreateGameDialog : FluentWindow
{
   
   private static Subject<int> _onClose = new();
   public static IObservable<int> CloseObservable => _onClose.AsObservable();
   
   private string _openFileDialog = "";
   private int OpenNum { get; set; }
    public CreateGameDialog()
    {
        InitializeComponent();
        LoggerController.LogInfo("CreateGameDialog Loaded");
        this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
    }

    public CreateGameDialog(string path)
    {
        InitializeComponent();
        LoggerController.LogInfo("CreateGameDialog Loaded");
        this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        PathLabel.Content = path;
        Label.Text = Path.GetFileNameWithoutExtension(path);
    }
    
    private void UrlButton_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            using (var ofd = new CommonOpenFileDialog())
            {
                ofd.Title = LocalizeControl.GetLocalize<string>("SelectFileLabel");
                ofd.IsFolderPicker = false;
                ofd.RestoreDirectory = true;
                if (ofd.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    try
                    {
                        _openFileDialog = ofd.FileName;
                        PathLabel.Content = _openFileDialog;
                        Label.Text = Path.GetFileNameWithoutExtension(_openFileDialog);
                        this.Activate();
                    }
                    catch (Exception exception)
                    {
                        Console.WriteLine(exception);
                        LoggerController.LogError("An I/O error occurred: " + exception.Message);
                        throw;
                    }
                }
                else
                {
                    LoggerController.LogWarn("User Cancelled File Selection");
                    ErrLabel.Visibility = Visibility.Visible;
                    Timer timer = new Timer(3000);
                    timer.Elapsed += (_,_) =>
                    {
                        this.Dispatcher.Invoke(() => { ErrLabel.Visibility = Visibility.Collapsed; });
                        timer.Stop();
                    };
                    timer.Start();
                    this.Activate();
                }

                ;
            }
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
            LoggerController.LogError("An I/O error occurred: " + exception.Message);
            throw;
        }
    }

    private void ExitBtn_OnClick(object sender, RoutedEventArgs e)
    {
        this.Close();
        LoggerController.LogInfo("CreateGameDialog Closed");
    }

    private void CreateGameDialog_OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
            this.DragMove();
    }

    private async void CreateButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (GenreSelectComboBox.SelectedItem == GenreApplicationComboBoxItem)
        {
            try
            {
                var fileExtension = Path.GetExtension(_openFileDialog);
                var fileExtensionDotTrim = fileExtension?.TrimStart('.');
                if (fileExtensionDotTrim is "")
                {
                    MessageBox.Show(LocalizeControl.GetLocalize<string>("SelectFileErrorMessage"), "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else if (_openFileDialog is "")
                {
                    MessageBox.Show(LocalizeControl.GetLocalize<string>("NotSelectFileError"), "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else if (Label.Text == LocalizeControl.GetLocalize<string>("NameInput"))
                {
                    MessageBox.Show(LocalizeControl.GetLocalize<string>("SelectFileErrorMessage"), "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }

                else
                {
                    JsonControl.ApplicationJsonData data = new JsonControl.ApplicationJsonData()
                    {
                        FilePath = _openFileDialog,
                        FileExtension = fileExtensionDotTrim,
                        Url = "",
                        Name = Label.Text,
                        JsonPath = $"{FileControl.Main.Directory}\\{Label.Text}.json",
                        Memo = "",
                        IsWebView = false,
                        IsUseLog = false,
                        MultipleLaunch = new []{""}
                    };
                    await JsonControl.CreateExeJson($"{FileControl.Main.Directory}\\{Label.Text}.json", data);

                    _onClose.OnNext(0);
                    this.Close();
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                throw;
            }
        }
        else if (GenreSelectComboBox.SelectedItem == GenreWebSiteComboBoxItem)
        {
            if (UrlBlock.Text.Contains( "http://") || UrlBlock.Text.Contains("https://"))
            {
                JsonControl.ApplicationJsonData data = new JsonControl.ApplicationJsonData()
                {
                    FilePath = _openFileDialog,
                    JsonPath = $"{FileControl.Main.Directory}\\{Label.Text}.json",
                    FileExtension = "web",
                    Name = Label.Text,
                    Url = UrlBlock.Text,
                    Memo = "",
                    IsWebView = false,
                    IsUseLog = false,
                    MultipleLaunch = new []{""}
                };
                await JsonControl.CreateExeJson($"{FileControl.Main.Directory}\\{Label.Text}.json", data);
            }
            else
            {
                MessageBox.Show(LocalizeControl.GetLocalize<string>("NotContainsHttpError"), "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            {
                
            }

            _onClose.OnNext(0);
            this.Close();
        }
        
        else if (GenreSelectComboBox.SelectedItem == GenreWebGameComboBoxItem)
        {
            if (UrlBlock.Text.Contains( "http://") || UrlBlock.Text.Contains("https://"))
            {
                JsonControl.ApplicationJsonData data = new JsonControl.ApplicationJsonData()
                {
                    FilePath = _openFileDialog,
                    JsonPath = $"{FileControl.Main.Directory}\\{Label.Text}.json",
                    FileExtension = "WebGame",
                    Url = UrlBlock.Text,
                    Name = Label.Text,
                    Memo = "",
                    IsWebView = false,
                    IsUseLog = false,
                    MultipleLaunch = new []{""}
                };
                await JsonControl.CreateExeJson($"{FileControl.Main.Directory}\\{Label.Text}.json", data);
            }
            else
            {
                MessageBox.Show(LocalizeControl.GetLocalize<string>("NotContainsHttpError"), "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            _onClose.OnNext(0);
            this.Close();
        }
        
        else if (GenreSelectComboBox.SelectedItem == GenreWebSaverComboBoxItem)
        {
            if (UrlBlock.Text.Contains("http://") || UrlBlock.Text.Contains("https://"))
            {
                try
                {
                    string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                    string htmlPath = Path.Combine(baseDirectory, "html");
                    var handler = new HttpClientHandler();
                    handler.UseCookies = true;
                    handler.CookieContainer = new CookieContainer();
                    using (HttpClient client = new HttpClient(handler))
                    {
                        var response = client.GetAsync(UrlBlock.Text).Result;
                        var content = response.Content.ReadAsStringAsync().Result;
                        using (var writer = new StreamWriter($"{htmlPath}/{Label.Text}.html"))
                        {
                            await writer.WriteLineAsync(content);
                        }
                    }
                    JsonControl.ApplicationJsonData data = new JsonControl.ApplicationJsonData()
                    {
                        FilePath = $"{htmlPath}/{Label.Text}.html",
                        JsonPath = $"{FileControl.Main.Directory}\\{Label.Text}.json",
                        FileExtension = "WebSaver",
                        Name = Label.Text,
                        MultipleLaunch = new []{""},
                        Url = UrlBlock.Text,
                        Memo = "",
                        IsWebView = false,
                        IsUseLog = false
                    };
                    await JsonControl.CreateExeJson($"{FileControl.Main.Directory}\\{Label.Text}.json", data);
                    
                }
                catch (Exception exception)
                {
                   LoggerController.LogError(exception.Message);
                }
                _onClose.OnNext(0);
                this.Close();
            }
            else
            {
                MessageBox.Show(LocalizeControl.GetLocalize<string>("NotContainsHttpError"), "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        else
        {
            MessageBox.Show(LocalizeControl.GetLocalize<string>("SelectGenreError"), "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void GenreSelectComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        switch (OpenNum)
        {
            case 1 when GenreSelectComboBox.SelectedItem == GenreWebSiteComboBoxItem:
                UrlButton.Visibility = Visibility.Collapsed;
                PathLabel.Visibility = Visibility.Collapsed;
                UrlBlock.Visibility = Visibility.Visible;
                break;
            case 1 when GenreSelectComboBox.SelectedItem == GenreApplicationComboBoxItem:
                UrlButton.Visibility = Visibility.Visible;
                PathLabel.Visibility = Visibility.Visible;
                UrlBlock.Visibility = Visibility.Collapsed;
                break;
            case 1 when GenreSelectComboBox.SelectedItem == GenreWebGameComboBoxItem:
                UrlButton.Visibility = Visibility.Collapsed;
                PathLabel.Visibility = Visibility.Collapsed;
                UrlBlock.Visibility = Visibility.Visible;
                break;
            case 1 when GenreSelectComboBox.SelectedItem == GenreWebSaverComboBoxItem:
                UrlButton.Visibility = Visibility.Collapsed;
                PathLabel.Visibility = Visibility.Collapsed;
                UrlBlock.Visibility = Visibility.Visible;
                break;
            case 1:
                UrlButton.Visibility = Visibility.Collapsed;
                PathLabel.Visibility = Visibility.Collapsed;
                UrlBlock.Visibility = Visibility.Collapsed;
                Label.Visibility = Visibility.Collapsed;
                break;
        }
    }

    private void GenreSelectComboBox_OnLoaded(object sender, RoutedEventArgs e)
    {
        GenreSelectComboBox.SelectedIndex = 0;
        try
        {
            LoggerController.LogInfo("GenreSelectComboBox Loaded");
            GenreLabel.Visibility = Visibility.Visible;
            Timer timer = new Timer(3000);
            timer.Elapsed += (_,_) =>
            {
                this.Dispatcher.Invoke(() => { GenreLabel.Visibility = Visibility.Collapsed; });
                timer.Stop();
            };
            timer.Start();
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
            throw;
        }
        OpenNum += 1;
    }
}