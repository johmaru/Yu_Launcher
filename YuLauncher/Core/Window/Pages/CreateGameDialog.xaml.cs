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
    public CreateGameDialog()
    {
        InitializeComponent();
        LoggerController.LogInfo("CreateGameDialog Loaded");
        this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
    }

    private void UrlButton_OnClick(object sender, RoutedEventArgs e)
    {
        using (var ofd = new CommonOpenFileDialog()
        {
            Title = LocalizeControl.GetLocalize<string>("SelectFileLabel"),
            IsFolderPicker = false,
            RestoreDirectory = true,
        })
            if (ofd.ShowDialog() == CommonFileDialogResult.Ok)
            {
                var fileExtension = Path.GetExtension(ofd.FileName);
                if (fileExtension is "")
                {
                    MessageBox.Show(LocalizeControl.GetLocalize<string>("SelectFileErrorMessage"), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else if (ofd.FileName == LocalizeControl.GetLocalize<string>("SelectFileLabel"))
                {
                    MessageBox.Show(LocalizeControl.GetLocalize<string>("SelectFileErrorMessage"), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                
                else
                {
                    using (var writer = new StreamWriter($"{FileControl.Main.Directory}\\{Label.Text}.txt"))
                    {
                        string[] test = new[] {ofd.FileName, fileExtension};
                        foreach (var t in test)
                        {
                            writer.WriteLine(t);
                        }
                    }
                    OnClose?.Invoke(this, EventArgs.Empty);
                    this.Close();
                }
            }
            else
            {
                LoggerController.LogWarn("User Cancelled File Selection");
                ErrLabel.Visibility = Visibility.Visible;
                Timer timer = new Timer(3000);
                timer.Elapsed += (sender, args) =>
                {
                    this.Dispatcher.Invoke(() =>
                    {
                        ErrLabel.Visibility = Visibility.Hidden;
                    });
                    timer.Stop();
                };
                timer.Start();
                this.Activate();
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
}