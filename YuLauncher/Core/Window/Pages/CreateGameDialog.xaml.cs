using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using Wpf.Ui.Controls;
using YuLauncher.Core.lib;
using Timer = System.Timers.Timer;

namespace YuLauncher.Core.Window.Pages;

public partial class CreateGameDialog : FluentWindow
{
    public CreateGameDialog()
    {
        InitializeComponent();
    }

    private void UrlButton_OnClick(object sender, RoutedEventArgs e)
    {
        using (var ofd = new CommonOpenFileDialog()
        {
            Title = "Select Game File",
            IsFolderPicker = false,
            RestoreDirectory = true,
        })
            if (ofd.ShowDialog() == CommonFileDialogResult.Ok)
            {
                using (var writer = new StreamWriter($"{FileControl.Main.Directory}\\{Label.Text}.txt"))
                {
                    writer.WriteLine(ofd.FileName);
                }
                this.Close();
            }
            else
            {
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
    }

    private void CreateGameDialog_OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
            this.DragMove();
    }
}