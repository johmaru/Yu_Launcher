using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Wpf.Ui.Controls;
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
                  try
                  {
                      CreateGameDialog createGameDialog = new CreateGameDialog();
                      createGameDialog.Show();
                  }
                  catch (Exception e)
                  {
                      LoggerController.LogError(e.Message);
                  }
                };
                contextMenu.Items.Add(menu);
                MenuItem menu2 = new MenuItem()
                {
                    Header = LocalizeControl.GetLocalize<string>("DeleteGame"),
                };
                menu2.Click += (sender, args) =>
                {
                    try
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
                            Console.WriteLine(gameButtonName);
                            LoggerController.LogError("file not found");
                        }
                    }
                    catch (Exception e)
                    {
                        LoggerController.LogError(e.Message);
                        throw;
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
                    try
                    {
                        CreateGameDialog createGameDialog = new CreateGameDialog();
                        createGameDialog.Show();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        LoggerController.LogError(e.Message);
                        throw;
                    }
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
    public Button GameButtonShow(string name,string[] path,string extension)
    {
        string[] tag = {name, path[0], extension};
        Console.WriteLine(extension);
        string thisFile = "./Games/" + name + ".txt";
        Button gameButton = new Button()
        {
            Content = name,
            Tag = tag,
            Height = ObjectProperty.GameListObjectHeight,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            ContextMenu = PageControlCreate.GameListShowContextMenu(true, thisFile),
        };
        gameButton.Click += (sender, args) => {
            try
            {
                switch (extension)
                {
                    case "exe":
                        Process.Start(path[0]);
                        break;
                    case "":
                        break;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                LoggerController.LogError(e.Message);
                throw;
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