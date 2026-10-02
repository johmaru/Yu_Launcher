using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
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
                            GameRepository.DeleteGameByJsonPath(data.JsonPath);
                            _deleteFileMenuClicked.OnNext(0);
                        }
                        else
                        {
                            if (GameRepository.ExistsByJsonPath(data.JsonPath))
                            {
                                GameRepository.DeleteGameByJsonPath(data.JsonPath);
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
                    if (!GameRepository.ExistsByJsonPath(data.JsonPath)) return;
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
                    if (!GameRepository.ExistsByJsonPath(data.JsonPath)) return;
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
                GameRepository.RecordPlay(data.Id);
                break;
            case "web":
                LaunchWeb(data);
                GameRepository.RecordPlay(data.Id);
                break;
            case "WebGame":
                new GameWindow(data.Url, data.JsonPath).Show();
                GameRepository.RecordPlay(data.Id);
                break;
            case "WebSaver":
                new WebSaverWindow.WebSaverWindow(data.Name, data).Show();
                GameRepository.RecordPlay(data.Id);
                break;
            case "":
                break;
        }
    }

    internal static BitmapSource? GetImage(JsonControl.ApplicationJsonData appData)
    {
        // WebGame, WebSaver, web all use favicon from URL
        // sz=128 で高解像度版を要求（デフォルトは 16x16 でガビガビになる）
        if (appData.FileExtension is "WebGame" or "WebSaver" or "web")
        {
            BitmapImage bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri("https://www.google.com/s2/favicons?domain=" + appData.Url + "&sz=128");
            bitmap.EndInit();
            return bitmap;
        }

        if (!File.Exists(appData.FilePath)) return null;

        IntPtr hIcon = IntPtr.Zero;
        bool comInitialized = false;
        try
        {
            int result = CoInitializeEx(IntPtr.Zero, 0x2); // COINIT_APARTMENTTHREADED
            if (result is not (0 or 1)) return null;
            comInitialized = true;

            SHFILEINFOW info = new();
            IntPtr shellResult = SHGetFileInfoW(appData.FilePath, 0, ref info,
                (uint)Marshal.SizeOf<SHFILEINFOW>(), 0x100); // SHGFI_ICON | SHGFI_LARGEICON
            hIcon = info.hIcon;
            if (shellResult == IntPtr.Zero || hIcon == IntPtr.Zero) return null;

            // Fully decode before releasing HICON; a direct WPF source can stall the list after DestroyIcon.
            BitmapSource source = Imaging.CreateBitmapSourceFromHIcon(hIcon, Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(source));
            using var stream = new MemoryStream();
            encoder.Save(stream);
            stream.Position = 0;
            BitmapImage image = new BitmapImage();
            image.BeginInit();
            image.StreamSource = stream;
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.EndInit();
            image.Freeze();
            return image;
        }
        catch (Exception ex)
        {
            LoggerController.LogError($"{ex}");
            return null;
        }
        finally
        {
            try
            {
                if (hIcon != IntPtr.Zero) DestroyIcon(hIcon);
            }
            finally
            {
                if (comInitialized) CoUninitialize();
            }
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct SHFILEINFOW
    {
        public IntPtr hIcon;
        public int iIcon;
        public uint dwAttributes;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string? szDisplayName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
        public string? szTypeName;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, EntryPoint = "SHGetFileInfoW")]
    private static extern IntPtr SHGetFileInfoW(string pszPath, uint dwFileAttributes,
        ref SHFILEINFOW psfi, uint cbFileInfo, uint uFlags);

    [DllImport("ole32.dll")]
    private static extern int CoInitializeEx(IntPtr pvReserved, uint dwCoInit);

    [DllImport("ole32.dll")]
    private static extern void CoUninitialize();

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyIcon(IntPtr hIcon);

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
