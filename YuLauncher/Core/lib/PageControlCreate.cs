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
using Image = System.Windows.Controls.Image;
using IndexOutOfRangeException = System.IndexOutOfRangeException;

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
    
    private ValueTask StartProcessWithLogging(string fileName, string name, string[] path)
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

    private BitmapImage GetImage(string[] path)
    {
        switch (path[1])
        {
            case "WebGame":
            {
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri("https://www.google.com/s2/favicons?domain=" + path[0]);
                bitmap.EndInit();
                return bitmap;
            }
            case "WebSaver":
            {
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri("https://www.google.com/s2/favicons?domain=" + path[0]);
                bitmap.EndInit();
                return bitmap;
            }
        }

        if (path[1] != "web")
        {
            if (File.Exists(path[0]))
            {
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    Icon icon = System.Drawing.Icon.ExtractAssociatedIcon(path[0]);
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
            bitmap.UriSource = new Uri("https://www.google.com/s2/favicons?domain=" + path[0]);
            bitmap.EndInit();
            return bitmap;
        }

        return null;
    }

    private int CountLines(string filePath)
    {
        using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        using (var reader = new StreamReader(stream))
        {
            int lineCount = 0;
            while (reader.ReadLine() != null)
            {
                lineCount++;
            }
            return lineCount;
        }
    }

    private async Task<bool> FirstLineCheck(string filePath)
    {
        return await Task.Run(() =>
        {
            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read,FileShare.ReadWrite))
            using (var reader = new StreamReader(stream))
            {
                return reader.ReadLine() == "";
            }
        });
    }

    private async Task UpdateFileAsync(string filePath)
    {
        int lineCount = await Task.Run(() => CountLines(filePath));
        Console.WriteLine(lineCount);
        using (var stream = new FileStream(filePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))        
        using (StreamWriter writer = new StreamWriter(stream))
        {
            bool isFirstLineEmpty = await Task.Run(() => FirstLineCheck(filePath));
            if (isFirstLineEmpty)
            {
                switch (lineCount)
                {
                    case 2:
                        await writer.WriteLineAsync("false");
                        await writer.WriteLineAsync("false");
                        break;
                    case 3:
                        await writer.WriteLineAsync("false");
                        break;
                }
            }
            else
            {
                switch (lineCount)
                {
                    case 2:
                        await writer.WriteLineAsync("");
                        await writer.WriteLineAsync("false");
                        await writer.WriteLineAsync("false");
                        break;
                    case 3:
                        await writer.WriteLineAsync("false");
                        await writer.WriteLineAsync("false");
                        break;
                    case 4:
                        await writer.WriteLineAsync("false");
                        break;
                }
            }
        }
    }

    private async Task TrimEndLinesAsync(string filePath)
    {
        var lines = await File.ReadAllLinesAsync(filePath);
        if (lines.Length > 0 && string.IsNullOrWhiteSpace(lines[^1]))
        {
            lines = lines.Take(lines.Length - 1).ToArray();
            await File.WriteAllLinesAsync(filePath, lines);
        }
    }
    public Button GameButtonShow(string name,string[] path,string extension)
    {
        
        string[] tag = {name, path[0], extension};
        string thisFile = "./Games/" + name + ".txt";
        Image image = new Image
        {
            Source = GetImage(path)
        };
        TextBlock textBlock = new TextBlock
        {
            Text = name + $" : {extension}",
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
            Tag = tag,
            Height = ObjectProperty.GameListObjectHeight,
            Width = ObjectProperty.GameListObjectWidth,
            HorizontalAlignment = HorizontalAlignment.Center,
            ContextMenu = PageControlCreate.GameListShowContextMenu(true, thisFile,path,name),
        };
        gameButton.Click += async (sender, args) => {
            try
            {
                switch (extension)
                {
                    case "exe":
                        if (!File.Exists(path[0]))
                        {
                            MessageBox.Show(LocalizeControl.GetLocalize<string>("SimpleFileNotFound"));
                            LoggerController.LogError("File not found");
                        }

                        if (path[4] == "true")
                        {
                            StartProcessWithLogging(path[0], name, path);
                        }

                        else
                        {
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
                                LoggerController.LogInfo(
                                    "Process has already been disposed(In most cases, this is normal behavior)");
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
            catch (IndexOutOfRangeException e)
            {
                MessageBox.Show("this file is old system on file and you using old version of file now optimized file please relaunch the app");
                LoggerController.LogError(e.Message);

                await UpdateFileAsync(thisFile);
                await TrimEndLinesAsync(thisFile);
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