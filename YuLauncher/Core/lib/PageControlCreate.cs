using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Wpf.Ui.Controls;
using YuLauncher.Core.Window;
using YuLauncher.Core.Window.Pages;
using YuLauncher.Game.Window;
using YuLauncher.WebSite;
using Button = Wpf.Ui.Controls.Button;
using GameWindow = YuLauncher.Game.Window.GameWindow;
using MenuItem = Wpf.Ui.Controls.MenuItem;
using MessageBox = System.Windows.MessageBox;

namespace YuLauncher.Core.lib;

public class PageControlCreate : Page
{
    public static event EventHandler OnDeleteFileMenuClicked;
    public static ContextMenu GameListShowContextMenu(bool isGameButton, string gameButtonPathFile,string[] data,string name)
    {
        ContextMenu contextMenu = new ContextMenu();
        
        switch (isGameButton)
        {
            case true:
            {
                MenuItem addCtx = new MenuItem()
                {
                    Header = LocalizeControl.GetLocalize<string>("AddGame"),
                };
                addCtx.Click += (sender, args) =>
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
                contextMenu.Items.Add(addCtx);
                
                MenuItem deleteCtx = new MenuItem()
                {
                    Header = LocalizeControl.GetLocalize<string>("DeleteGame"),
                };
                deleteCtx.Click += (sender, args) =>
                {
                    try
                    {
                        string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                        string htmlPath = Path.Combine(baseDirectory, $"html/{name}.html");
                        if (data[1] == "WebSaver")
                        {
                            File.Delete(htmlPath);
                            FileControl.DeleteGame(gameButtonPathFile);
                            OnDeleteFileMenuClicked?.Invoke(null, EventArgs.Empty);
                            
                        }
                        else
                        {
                            if (FileControl.ExistGameFile(gameButtonPathFile))
                            {
                                FileControl.DeleteGame(gameButtonPathFile);
                                LoggerController.LogWarn($"delete file: {gameButtonPathFile}");
                                Console.WriteLine("delete file");
                                OnDeleteFileMenuClicked?.Invoke(null, EventArgs.Empty);
                            }
                            else
                            {
                                Console.WriteLine("file not found");
                                Console.WriteLine(gameButtonPathFile);
                                LoggerController.LogError("file not found");
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        LoggerController.LogError(e.Message);
                        throw;
                    }
                };
                contextMenu.Items.Add(deleteCtx);
                MenuItem memoCtx = new MenuItem()
                {
                    Header = LocalizeControl.GetLocalize<string>("MemoCtxHeader")
                };
                memoCtx.Click += (sender, args) =>
                {
                    if (File.Exists(gameButtonPathFile))
                    {
                        string[] memo = File.ReadAllLines(gameButtonPathFile);
                        try
                        {
                            MemoWindow memoWindow = new MemoWindow(gameButtonPathFile,memo);
                            memoWindow.Show();
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                            throw;
                        }
                    }
                };
                contextMenu.Items.Add(memoCtx);
                MenuItem propertyCtx = new MenuItem()
                {
                    Header = LocalizeControl.GetLocalize<string>("PropertyCtxHeader")
                };
                propertyCtx.Click += (sender, args) =>
                {
                    if (File.Exists(gameButtonPathFile))
                    {
                        try
                        {
                            PropertyDialog propertyDialog = new PropertyDialog(data,name,gameButtonPathFile);
                            propertyDialog.Show();
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                            throw;
                        }
                    }
                };
                contextMenu.Items.Add(propertyCtx);
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
        string thisFile = "./Games/" + name + ".txt";
        Button gameButton = new Button()
        {
            Content = name,
            Tag = tag,
            Height = ObjectProperty.GameListObjectHeight,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            ContextMenu = PageControlCreate.GameListShowContextMenu(true, thisFile,path,name),
        };
        gameButton.Click += (sender, args) => {
            try
            {
                    Console.WriteLine("File found");
                    switch (extension)
                    {
                        case "exe":
                            if (!File.Exists(path[0]))
                            {
                                MessageBox.Show(LocalizeControl.GetLocalize<string>("SimpleFileNotFound"));
                                LoggerController.LogError("File not found");
                            }
                            ProcessStartInfo startInfo = new ProcessStartInfo
                            {
                                FileName = path[0], 
                                RedirectStandardOutput = true, 
                                RedirectStandardError = true, 
                                UseShellExecute = false, 
                            };
                            Process process = new Process
                            {
                                StartInfo = startInfo,
                            };
                            process.OutputDataReceived += (sender, e) =>
                            {
                                LoggerController.LogDebug("Output: " + e.Data);
                            };
                            process.ErrorDataReceived += (sender, e) =>
                            {
                                LoggerController.LogWarn("Error: " + e.Data);
                            };
                            try
                            {
                                process.Start();
                            
                            }
                            catch (ObjectDisposedException)
                            {
                                LoggerController.LogInfo("Process has already been disposed(In most cases, this is normal behavior)");
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e);
                                MessageBox.Show($"{LocalizeControl.GetLocalize<string>("FileCantOpen")} :{e.Message}");
                                throw;
                            }
                            process.BeginOutputReadLine();
                            process.BeginErrorReadLine();

                            process.WaitForExit();
                            break;
                        case "web":
                            if (path[3] == "true")
                            {
                                WebViewWindow webViewWindow = new WebViewWindow(path[0], path);
                                webViewWindow.Show();
                            }
                            else
                            {
                                ProcessStartInfo websiteInfo = new ProcessStartInfo
                                {
                                    FileName = path[0],
                                    UseShellExecute = true,
                                };
                                Process.Start(websiteInfo);
                            }
                            break;
                        case "WebGame":
                        GameWindow gameWindow = new GameWindow(path[0], path);
                        gameWindow.Show();
                        break;
                        case "WebSaver":
                            WebSaverWindow.WebSaverWindow webSaverWindow = new WebSaverWindow.WebSaverWindow(name, path);
                            webSaverWindow.Show();
                            break;
                        case "":
                        break;
                    }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                LoggerController.LogError(e.Message);
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