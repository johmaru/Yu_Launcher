using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using YuLauncher.Core.Window.Pages;
using Button = Wpf.Ui.Controls.Button;
using MenuItem = Wpf.Ui.Controls.MenuItem;

namespace YuLauncher.Core.lib;

public class PageControlCreate : Page
{
    public static event EventHandler OnDeleteFileMenuClicked;
    public static ContextMenu GameListShowContextMenu(bool isGameButton, string gameButtonName)
    {
        ContextMenu contextMenu = new ContextMenu();
        switch (isGameButton)
        {
            case true:
            {
                MenuItem menu = new MenuItem()
                {
                    Header = LocalizeControl.GetLocalize<string>("AddGame"),
                };
                menu.Click += (sender, args) =>
                {
                    CreateGameDialog createGameDialog = new CreateGameDialog();
                    createGameDialog.Show();
                };
                contextMenu.Items.Add(menu);
                MenuItem menu2 = new MenuItem()
                {
                    Header = LocalizeControl.GetLocalize<string>("DeleteGame"),
                };
                menu2.Click += (sender, args) =>
                {
                    if (FileControl.ExistGameFile(gameButtonName))
                    {
                        FileControl.DeleteGame(gameButtonName);
                        LoggerController.LogWarn($"delete file: {gameButtonName}");
                        Console.WriteLine("delete file");
                        OnDeleteFileMenuClicked?.Invoke(null, EventArgs.Empty);
                    }
                    else
                    {
                        Console.WriteLine("file not found");
                        LoggerController.LogError("file not found");
                    }
                };
                contextMenu.Items.Add(menu2);
                break;
            }
            case false:
            {
                MenuItem menu = new MenuItem()
                {
                    Header = LocalizeControl.GetLocalize<string>("AddGame"),
                };
                menu.Click += (sender, args) =>
                {
                    CreateGameDialog createGameDialog = new CreateGameDialog();
                    createGameDialog.Show();
                };

                contextMenu.Items.Add(menu);
                break;
            }
        }
        return contextMenu;
    }
}

public class GameButton : Button
{
    public bool IsMouseEntered { get; private set; }
    public Button GameButtonShow(string name,string path,string extension)
    {
        string[] tag = {name, path, extension};
        string thisFile = "./Games/" + name + extension;
        Button gameButton = new Button()
        {
            Content = name,
            Tag = tag,
            Margin = new Thickness(5),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            ContextMenu = PageControlCreate.GameListShowContextMenu(true, thisFile),
        };
        gameButton.Click += (sender, args) => {
            switch (extension)
            {
                case ".exe":
                    System.Diagnostics.Process.Start(path);
                    break;
                case "":
                    break;
            }
        };
        gameButton.MouseEnter += (sender, args) =>
        {
            IsMouseEntered = true;
        };
        gameButton.MouseLeave += (sender, args) =>
        {
            IsMouseEntered = false;
        };
        return gameButton;
    }
}