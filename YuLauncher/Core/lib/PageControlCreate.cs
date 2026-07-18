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
using YuLauncher.Core.Window;
using YuLauncher.Core.Window.Pages;
using YuLauncher.WebSite;
using Button = Wpf.Ui.Controls.Button;
using GameWindow = YuLauncher.Game.Window.GameWindow;
using MenuItem = Wpf.Ui.Controls.MenuItem;
using MessageBox = System.Windows.MessageBox;
using TextBlock = Wpf.Ui.Controls.TextBlock;
using System.Reactive.Subjects;
using Image = System.Windows.Controls.Image;

namespace YuLauncher.Core.lib;

public static class PageControlCreate
{
    private static readonly Subject<int> _deleteFileMenuClicked = new();
    public static IObservable<int> DeleteFileMenuClicked => _deleteFileMenuClicked;

    public static ContextMenu GameListShowContextMenu(bool isGameButton, JsonControl.ApplicationJsonData data)
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
                addCtx.Click += (_, _) =>
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
                deleteCtx.Click += (_, _) =>
                {
                    try
                    {
                        string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                        string htmlPath = Path.Combine(baseDirectory, $"html/{data.Name}.html");
                        if (data.FileExtension == "WebSaver")
                        {
                            File.Delete(htmlPath);
                            File.Delete(data.JsonPath);
                            _deleteFileMenuClicked.OnNext(0);
                        }
                        else
                        {
                            if (File.Exists(data.JsonPath))
                            {
                                File.Delete(data.JsonPath);
                                LoggerController.LogWarn($"delete file: {data.JsonPath}");
                                _deleteFileMenuClicked.OnNext(0);
                            }
                            else
                            {
                                LoggerController.LogError($"file not found: {data.JsonPath}");
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        LoggerController.LogError(e.Message);
                    }
                };
                contextMenu.Items.Add(deleteCtx);

                MenuItem memoCtx = new MenuItem()
                {
                    Header = LocalizeControl.GetLocalize<string>("MemoCtxHeader")
                };
                memoCtx.Click += (_, _) =>
                {
                    if (!File.Exists(data.JsonPath)) return;
                    try
                    {
                        MemoWindow memoWindow = new MemoWindow(data);
                        memoWindow.Show();
                    }
                    catch (Exception e)
                    {
                        LoggerController.LogError(e.Message);
                    }
                };
                contextMenu.Items.Add(memoCtx);

                MenuItem propertyCtx = new MenuItem()
                {
                    Header = LocalizeControl.GetLocalize<string>("PropertyCtxHeader")
                };
                propertyCtx.Click += (_, _) =>
                {
                    if (!File.Exists(data.JsonPath)) return;
                    try
                    {
                        PropertyDialog propertyDialog = new PropertyDialog(data);
                        propertyDialog.Show();
                    }
                    catch (Exception e)
                    {
                        LoggerController.LogError(e.Message);
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
                menu.Click += (_, _) =>
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
                break;
            }
        }
        return contextMenu;
    }
}

public class GameButton : Button
{
    public bool IsMouseEntered { get; private set; }

    private static void EnsureLogDirectories(string name)
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

    private static async Task LaunchExe(JsonControl.ApplicationJsonData data)
    {
        if (!File.Exists(data.FilePath))
        {
            MessageBox.Show(LocalizeControl.GetLocalize<string>("SimpleFileNotFound"));
            LoggerController.LogError($"file not found: {data.FilePath}");
            return;
        }

        if (data.IsUseLog == true)
        {
            await StartProcessWithLogging(data.FilePath, data.Name);
            return;
        }

        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = data.FilePath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        Process process = new Process { StartInfo = startInfo };
        process.OutputDataReceived += (_, e) => LoggerController.LogDebug($"Output: {e.Data}");
        process.ErrorDataReceived += (_, e) => LoggerController.LogWarn($"Error: {e.Data}");

        try
        {
            process.Start();
        }
        catch (Exception e)
        {
            MessageBox.Show($"{LocalizeControl.GetLocalize<string>("FileCantOpen")} :{e.Message}");
            LoggerController.LogError(e.Message);
            return;
        }

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        await process.WaitForExitAsync();
    }

    private static async Task StartProcessWithLogging(string fileName, string name)
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
        process.OutputDataReceived += (_, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                output.AppendLine($"Output :{e.Data}");
            }
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                output.AppendLine($"Error :{e.Data}");
            }
        };
        process.Exited += (_, _) =>
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
            LoggerController.LogInfo("Process has already been disposed (in most cases, this is normal behavior)");
        }
        catch (Exception e)
        {
            MessageBox.Show($"{LocalizeControl.GetLocalize<string>("FileCantOpen")} :{e.Message}");
            LoggerController.LogError(e.Message);
            return;
        }

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        await process.WaitForExitAsync();
    }

    private static void LaunchWeb(JsonControl.ApplicationJsonData data)
    {
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
    }

    internal static async Task LaunchApplication(JsonControl.ApplicationJsonData data)
    {
        switch (data.FileExtension)
        {
            case "exe":
                await LaunchExe(data);
                break;
            case "web":
                LaunchWeb(data);
                break;
            case "WebGame":
                new GameWindow(data.Url, data.JsonPath).Show();
                break;
            case "WebSaver":
                new WebSaverWindow.WebSaverWindow(data.Name, data).Show();
                break;
            case "":
                break;
        }
    }

    internal static BitmapImage? GetImage(JsonControl.ApplicationJsonData appData)
    {
        // WebGame, WebSaver, web all use favicon from URL
        if (appData.FileExtension is "WebGame" or "WebSaver" or "web")
        {
            BitmapImage bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri("https://www.google.com/s2/favicons?domain=" + appData.Url);
            bitmap.EndInit();
            return bitmap;
        }

        if (!File.Exists(appData.FilePath)) return null;
        using (MemoryStream memoryStream = new MemoryStream())
        {
            Icon? icon = System.Drawing.Icon.ExtractAssociatedIcon(appData.FilePath);
            if (icon != null) icon.Save(memoryStream);
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

    public Button GameButtonShow(string name, JsonControl.ApplicationJsonData data)
    {
        Image image = new Image { Source = GetImage(data) };

        TextBlock textBlock = new TextBlock
        {
            Text = name + $" : {data.FileExtension}",
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            FontSize = 12,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(5, 0, 0, 0)
        };

        StackPanel stackPanel = new StackPanel { Orientation = Orientation.Horizontal };
        stackPanel.Children.Add(image);
        stackPanel.Children.Add(textBlock);

        Button gameButton = new Button()
        {
            Content = stackPanel,
            Tag = data,
            Height = ObjectProperty.GameListObjectHeight,
            Width = ObjectProperty.GameListObjectWidth,
            HorizontalAlignment = HorizontalAlignment.Center,
            ContextMenu = PageControlCreate.GameListShowContextMenu(true, data),
        };

        gameButton.Click += async (_, _) =>
        {
            try
            {
                await LaunchApplication(data);

                if (data.MultipleLaunch is { Length: > 0 })
                {
                    foreach (var multipleLaunch in data.MultipleLaunch)
                    {
                        if (string.IsNullOrEmpty(multipleLaunch)) continue;
                        var multipleData = await JsonControl.ReadExeJson($"./Games/{multipleLaunch}.json");
                        await LaunchApplication(multipleData);
                    }
                }
            }
            catch (Exception e)
            {
                LoggerController.LogError(e.Message);
            }
        };

        gameButton.MouseEnter += (_, _) => IsMouseEntered = true;
        gameButton.MouseLeave += (_, _) => IsMouseEntered = false;

        return gameButton;
    }
}
