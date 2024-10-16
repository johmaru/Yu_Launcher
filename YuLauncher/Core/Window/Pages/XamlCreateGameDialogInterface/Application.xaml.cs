using System;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using YuLauncher.Core.lib;

namespace YuLauncher.Core.Window.Pages.XamlCreateGameDialogInterface;

public partial class Application : DialogInterface
{
    private Subject<int> _nameChangeSaveClicked = new Subject<int>();
    public IObservable<int> NameChangeSaveClicked => _nameChangeSaveClicked.AsObservable();
    public Application(JsonControl.ApplicationJsonData data) : base(data)
    {
        InitializeComponent();
        
        InterFace.SetNameLabel(NameTextBlock,InterFace.IsDark());
        InterFace.SetNameBox(NameBox,data.Name,InterFace.IsDark());
        
        InterFace.SetNameLabel(ExePathNameTextBlock,InterFace.IsDark());
        InterFace.SetPathBox(ExePathNameBox,NewPath,InterFace.IsDark());
        
        InterFace.SetNameLabel(MultipleLaunchText,InterFace.IsDark());
    }

    private async void SaveButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (NameBox.Text != NowName)
        {
            try
            {
                Data = Data with { Name = NameBox.Text };
                await JsonControl.CreateExeJson(Data.JsonPath,Data);
            }
            catch (Exception exception)
            {
                LoggerController.LogError($"{exception}");
            }
        }

        if (ExePathNameBox.Text != NewPath)
        {
            try
            {
                Data = Data with { FilePath = ExePathNameBox.Text };
                await JsonControl.CreateExeJson(Data.JsonPath, Data);
            }
            catch (Exception exception)
            {
                LoggerController.LogError($"{exception}");
            }
        }
        try
        {

            if (ApplicationLogButton.IsChecked == true)
            {
               Data = Data with { IsUseLog = true };
            }
            else
            {
              Data = Data with { IsUseLog = false };
            }
            try
            {
                await JsonControl.CreateExeJson(Data.JsonPath, Data);

                var data = await JsonControl.ReadExeJson(Data.JsonPath);
                var checkBox = MultiplePanel.Children.OfType<CheckBox>();
                foreach (var cb in checkBox)
                {
                    if (cb.IsChecked == true)
                    {
                        if (!data.MultipleLaunch.Contains(cb.Content.ToString()))
                        {
                            data = data with
                            {
                                MultipleLaunch = data.MultipleLaunch.Append(cb.Content.ToString()).ToArray()
                            };
                        }
                        else
                        {
                            continue;
                        }
                    }
                    else
                    {
                        data = data with
                        {
                            MultipleLaunch = data.MultipleLaunch.Where(x => x == (string)cb.Content).ToArray()
                        };
                    }

                    await JsonControl.CreateExeJson(data.JsonPath, data);
                }
            }
            catch (Exception exception)
            {
               LoggerController.LogError($"{exception}");
            }

            _nameChangeSaveClicked.OnNext(0);
        }
        catch (IndexOutOfRangeException ex)
        {
            MessageBox.Show("this file is old system on file and you using old version of file");
            LoggerController.LogError(ex.Message);
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
            throw;
        }
    }

    private async void ApplicationLog_OnInitialized(object? sender, EventArgs e)
    {
        try
        {
            ApplicationLogButton.IsChecked = Data.IsUseLog;
            
            var jsonFiles = Directory.GetFiles("./Games", "*.json");
            foreach (var jf in jsonFiles)
            {
                var data = await JsonControl.ReadExeJson(jf);
                if (data.Name == Data.Name)
                {
                    continue;
                }

                TextBlock text = new TextBlock()
                {
                    Text = data.Name,
                    FontSize = 20
                };

                var checkBox = new CheckBox()
                {
                    Content = text,
                    IsChecked = Data.MultipleLaunch.Contains(data.Name),
                    Tag = data.FilePath,
                };

                checkBox.SetBinding(WidthProperty, new Binding("ActualWidth")
                {
                    Source = this,
                    Mode = BindingMode.OneWay
                });

                MultiplePanel.Children.Add(checkBox);
            }
        }
        catch (IndexOutOfRangeException ex)
        {
            MessageBox.Show("this file is old system on file and you using old version of file");
            LoggerController.LogError(ex.Message);
        }
        catch (Exception exception)
        {
            LoggerController.LogError($"{exception}");
            throw;
        }
    }

    private void Application_OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        ExePathNameBox.Width = e.NewSize.Width - ExePathNameTextBlock.ActualWidth - 50;
        NameBox.Width = e.NewSize.Width - NameTextBlock.ActualWidth - 50;
    }
}