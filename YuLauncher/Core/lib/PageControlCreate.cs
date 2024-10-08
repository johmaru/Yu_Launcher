using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Wpf.Ui.Controls;
using YuLauncher.Core.Window;
using YuLauncher.Core.Window.Pages;
using YuLauncher.Game.Window;
using YuLauncher.WebSite;
using Button = Wpf.Ui.Controls.Button;
using GameWindow = YuLauncher.Game.Window.GameWindow;
using MenuItem = Wpf.Ui.Controls.MenuItem;
using MessageBox = System.Windows.MessageBox;
using TextBlock = Wpf.Ui.Controls.TextBlock;
using System.Drawing;
using System.Reactive.Subjects;
using Image = System.Windows.Controls.Image;
using IndexOutOfRangeException = System.IndexOutOfRangeException;

namespace YuLauncher.Core.lib;

public class PageControlCreate : Page
{
    private static Subject<int> _deleteFileMenuClicked = new Subject<int>();
    public static IObservable<int> DeleteFileMenuClicked => _deleteFileMenuClicked;
    public static ContextMenu GameListShowContextMenu(bool isGameButton,JsonControl.ApplicationJsonData data)
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
                        string htmlPath = Path.Combine(baseDirectory, $"html/{data.Name}.html");
                        if (data.FileExtension == "WebSaver")
                        {
                            File.Delete(htmlPath);
                            FileControl.DeleteGame(data.JsonPath);
                           _deleteFileMenuClicked.OnNext(0);
                            
                        }
                        else
                        {
                            if (FileControl.ExistGameFile(data.JsonPath))
                            {
                                FileControl.DeleteGame(data.JsonPath);
                                LoggerController.LogWarn($"delete file: {data.JsonPath}");
                                Console.WriteLine("delete file");
                               _deleteFileMenuClicked.OnNext(0);
                            }
                            else
                            {
                                Console.WriteLine("file not found");
                                Console.WriteLine(data.JsonPath);
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
                    if (File.Exists(data.JsonPath))
                    {
                        try
                        {
                            MemoWindow memoWindow = new MemoWindow(data);
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
                    if (File.Exists(data.JsonPath))
                    {
                        try
                        {
                            PropertyDialog propertyDialog = new PropertyDialog(data);
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
    
    private void EnsureLogDirectories(string name)
    {
        if (!Directory.Exists("./AppLogs"))
        {
            Directory.CreateDirectory("./AppLogs");
        }

        if (!Directory.Exists($"./AppLogs/{name}"))
        {
            Directory.CreateDirectory($"./AppLogs/{name}");
        }
    }
    
    private ValueTask StartProcessWithLogging(string fileName, string name)
    {
        EnsureLogDirectories(name);

        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        Process process = new Process
        {
            StartInfo = startInfo,
            EnableRaisingEvents = true,
        };

        StringBuilder output = new StringBuilder();
        process.OutputDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                output.AppendLine($"Output :{e.Data}");
            }
        };
        process.ErrorDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                output.AppendLine($"Error :{e.Data}");
            }
        };
        process.Exited += (sender, e) =>
        {
            string logPath = $"./AppLogs/{name}/{DateTime.Now:yyyy-MM-dd-HH-mm-ss}.txt";
            File.WriteAllLines(logPath, output.ToString().Split('\n').Where(x => x != "").ToArray());
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
        return ValueTask.CompletedTask;
    }

    private static BitmapImage GetImage(JsonControl.ApplicationJsonData appData)
    {
        switch (appData.FileExtension)
        {
            case "WebGame":
            {
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri("https://www.google.com/s2/favicons?domain=" + appData.Url);
                bitmap.EndInit();
                return bitmap;
            }
            case "WebSaver":
            {
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri("https://www.google.com/s2/favicons?domain=" + appData.Url);
                bitmap.EndInit();
                return bitmap;
            }
        }

        if (appData.FileExtension != "web")
        {
            if (File.Exists(appData.FilePath))
            {
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    Icon icon = System.Drawing.Icon.ExtractAssociatedIcon(appData.FilePath);
                    icon.Save(memoryStream);
                    memoryStream.Position = 0;

                    BitmapImage bitmapImage = new BitmapImage();
                    bitmapImage.BeginInit();
                    bitmapImage.StreamSource = memoryStream;
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.EndInit();
                    bitmapImage.Freeze();

                    return bitmapImage;
                }
            }
        }
        else
        {
            BitmapImage bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri("https://www.google.com/s2/favicons?domain=" + appData.Url);
            bitmap.EndInit();
            return bitmap;
        }

        return null;
    }
    public Button GameButtonShow(string name,JsonControl.ApplicationJsonData data)
    {
        string thisFile = "./Games/" + name + ".json";
            Image image = new Image
            {
                Source = GetImage(data)
            };
 
        TextBlock textBlock = new TextBlock
        {
            Text = name + $" : {data.FileExtension}",
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            FontSize = 12,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(5, 0, 0, 0)
        };

        StackPanel stackPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal
        };
        stackPanel.Children.Add(image);
        stackPanel.Children.Add(textBlock);
        Button gameButton = new Button()
        {
            Content = stackPanel,
            Tag = data,
            Height = ObjectProperty.GameListObjectHeight,
            Width = ObjectProperty.GameListObjectWidth,
            HorizontalAlignment = HorizontalAlignment.Center,
            ContextMenu = PageControlCreate.GameListShowContextMenu(true,data),
        };
        gameButton.Click += async (sender, args) => {
            try
            {
                switch (data.FileExtension)
                {
                    case "exe":
                        if (!File.Exists(data.FilePath))
                        {
                            MessageBox.Show(LocalizeControl.GetLocalize<string>("SimpleFileNotFound"));
                            LoggerController.LogError("File not found");
                        }

                        if (data.IsUseLog == true)
                        {
                                await StartProcessWithLogging(data.FilePath, data.Name);
                        }

                        else
                        {
                            
                            ProcessStartInfo startInfo = new ProcessStartInfo
                            {
                                FileName = data.FilePath,
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
                            catch (Exception e)
                            {
                                Console.WriteLine(e);
                                MessageBox.Show($"{LocalizeControl.GetLocalize<string>("FileCantOpen")} :{e.Message}");
                                throw;
                            }

                            process.BeginOutputReadLine();
                            process.BeginErrorReadLine();

                            await process.WaitForExitAsync();
                        }

                        break;
                    case "web":
                        if (data.IsWebView == true)
                        {
                            WebViewWindow webViewWindow = new WebViewWindow(data.Url, data);
                            webViewWindow.Show();
                        }
                        else
                        {
                            ProcessStartInfo websiteInfo = new ProcessStartInfo
                            {
                                FileName = data.Url,
                                UseShellExecute = true,
                            };
                            Process.Start(websiteInfo);
                        }

                        break;
                    case "WebGame":
                        GameWindow gameWindow = new GameWindow(data.Url, data);
                        gameWindow.Show();
                        break;
                    case "WebSaver":
                        WebSaverWindow.WebSaverWindow webSaverWindow = new WebSaverWindow.WebSaverWindow(name,data);
                        webSaverWindow.Show();
                        break;
                    case "":
                        break;
                }
                
                
                if (data.MultipleLaunch != null || data.MultipleLaunch.Length !=0) 
                {
                    foreach (var multipleLaunch in data.MultipleLaunch)
                    {
                        JsonControl.ApplicationJsonData multipleData = await JsonControl.ReadExeJson($"./Games/{multipleLaunch}.json");
                        switch (multipleData.FileExtension)
                        {
                            case "exe":
                                if (!File.Exists(multipleData.FilePath))
                                {
                                    MessageBox.Show(LocalizeControl.GetLocalize<string>("SimpleFileNotFound"));
                                    LoggerController.LogError("File not found");
                                }

                                if (multipleData.IsUseLog == true)
                                {
                                    await StartProcessWithLogging(multipleData.FilePath, multipleData.Name);
                                }

                                else
                                {
                                    ProcessStartInfo startInfo = new ProcessStartInfo
                                    {
                                        FileName = multipleData.FilePath,
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
                                    catch (Exception e)
                                    {
                                        Console.WriteLine(e);
                                        MessageBox.Show($"{LocalizeControl.GetLocalize<string>("FileCantOpen")} :{e.Message}");
                                        throw;
                                    }

                                    process.BeginOutputReadLine();
                                    process.BeginErrorReadLine();

                                    await process.WaitForExitAsync();
                                }

                                break;
                            case "web":
                                if (multipleData.IsWebView == true)
                                {
                                    WebViewWindow webViewWindow = new WebViewWindow(multipleData.Url, multipleData);
                                    webViewWindow.Show();
                                }
                                else
                                {
                                    ProcessStartInfo websiteInfo = new ProcessStartInfo
                                    {
                                        FileName = multipleData.Url,
                                        UseShellExecute = true,
                                    };
                                    Process.Start(websiteInfo);
                                }

                                break;
                            case "WebGame":
                                GameWindow gameWindow = new GameWindow(multipleData.Url, multipleData);
                                gameWindow.Show();
                                break;
                            case "WebSaver":
                                WebSaverWindow.WebSaverWindow webSaverWindow = new WebSaverWindow.WebSaverWindow(multipleData.Name,multipleData);
                                webSaverWindow.Show();
                                break;
                            case "":
                                break;
                        }
                    }
                }
                else
                {
                     switch (data.FileExtension)
                {
                    case "exe":
                        if (!File.Exists(data.FilePath))
                        {
                            MessageBox.Show(LocalizeControl.GetLocalize<string>("SimpleFileNotFound"));
                            LoggerController.LogError("File not found");
                        }

                        if (data.IsUseLog == true)
                        {
                                await StartProcessWithLogging(data.FilePath, data.Name);
                        }

                        else
                        {
                            
                            ProcessStartInfo startInfo = new ProcessStartInfo
                            {
                                FileName = data.FilePath,
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
                            catch (Exception e)
                            {
                                Console.WriteLine(e);
                                MessageBox.Show($"{LocalizeControl.GetLocalize<string>("FileCantOpen")} :{e.Message}");
                                throw;
                            }

                            process.BeginOutputReadLine();
                            process.BeginErrorReadLine();

                            await process.WaitForExitAsync();
                        }

                        break;
                    case "web":
                        if (data.IsWebView == true)
                        {
                            WebViewWindow webViewWindow = new WebViewWindow(data.Url, data);
                            webViewWindow.Show();
                        }
                        else
                        {
                            ProcessStartInfo websiteInfo = new ProcessStartInfo
                            {
                                FileName = data.Url,
                                UseShellExecute = true,
                            };
                            Process.Start(websiteInfo);
                        }

                        break;
                    case "WebGame":
                        GameWindow gameWindow = new GameWindow(data.Url, data);
                        gameWindow.Show();
                        break;
                    case "WebSaver":
                        WebSaverWindow.WebSaverWindow webSaverWindow = new WebSaverWindow.WebSaverWindow(name,data);
                        webSaverWindow.Show();
                        break;
                    case "":
                        break;
                }
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