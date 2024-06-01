using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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
   public static event EventHandler? OnClose;
   private string _openFileDialog = "";
   private int OpenNum { get; set; }
    public CreateGameDialog()
    {
        InitializeComponent();
        LoggerController.LogInfo("CreateGameDialog Loaded");
        this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
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
                    timer.Elapsed += (sender, args) =>
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

    private void CreateButton_OnClick(object sender, RoutedEventArgs e)
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
                    using (var writer = new StreamWriter($"{FileControl.Main.Directory}\\{Label.Text}.txt"))
                    {
                        string?[] test = new[] { _openFileDialog, fileExtensionDotTrim };
                        foreach (var t in test)
                        {
                            writer.WriteLine(t);
                        }
                    }

                    OnClose?.Invoke(this, EventArgs.Empty);
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
            using (var writer = new StreamWriter($"{FileControl.Main.Directory}\\{Label.Text}.txt"))
            {
                string?[] test = new[] { UrlBlock.Text, "web" };
                foreach (var t in test)
                {
                    writer.WriteLine(t);
                }
            }

            OnClose?.Invoke(this, EventArgs.Empty);
            this.Close();
        }
        else
        {
            MessageBox.Show(LocalizeControl.GetLocalize<string>("SelectGenreError"), "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void GenreSelectComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (OpenNum == 1)
        {
            if (GenreSelectComboBox.SelectedItem == GenreWebSiteComboBoxItem)
            {
                UrlButton.Visibility = Visibility.Collapsed;
                PathLabel.Visibility = Visibility.Collapsed;
                UrlBlock.Visibility = Visibility.Visible;
            }
            else if (GenreSelectComboBox.SelectedItem == GenreApplicationComboBoxItem)
            {
                UrlButton.Visibility = Visibility.Visible;
                PathLabel.Visibility = Visibility.Visible;
                UrlBlock.Visibility = Visibility.Collapsed;
            }
            else
            {
                UrlButton.Visibility = Visibility.Collapsed;
                PathLabel.Visibility = Visibility.Collapsed;
                UrlBlock.Visibility = Visibility.Collapsed;
                Label.Visibility = Visibility.Collapsed;
            }
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
            timer.Elapsed += (sender, args) =>
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