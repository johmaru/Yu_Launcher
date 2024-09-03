using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using YuLauncher.Core.lib;

namespace YuLauncher.Core.Window.Pages.XamlCreateGameDialogInterface;

public partial class Application : DialogInterface
{
    public static event EventHandler? OnNameChangeAppSaveClicked;
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
                Data = Data with{Name = NameBox.Text};            
                await JsonControl.CreateExeJson(Data.JsonPath,Data);
        }

        if (ExePathNameBox.Text != NewPath)
        {
            Data = Data with { FilePath = ExePathNameBox.Text };
            await JsonControl.CreateExeJson(Data.JsonPath,Data);
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
            await JsonControl.CreateExeJson(Data.JsonPath, Data);

            var data = await JsonControl.ReadExeJson(Data.JsonPath);
            var checkBox = MultiplePanel.Children.OfType<CheckBox>();
            foreach (var CB in checkBox)
            {
                 if (CB.IsChecked == true)
                 {
                     if (!data.MultipleLaunch.Contains(CB.Content.ToString()))
                     {
                         data = data with { MultipleLaunch = data.MultipleLaunch.Append(CB.Content.ToString()).ToArray() };
                     }
                     else
                     {
                         continue;
                     }
                 }
                 else
                 {
                     data = data with { MultipleLaunch = data.MultipleLaunch.Where(x => x == (string)CB.Content).ToArray() };
                 }

                 await JsonControl.CreateExeJson(data.JsonPath, data);
            }

            OnNameChangeAppSaveClicked?.Invoke(this, EventArgs.Empty);
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
            if (Data.IsUseLog)
            {
                ApplicationLogButton.IsChecked = true;
            }
            else
            {
                ApplicationLogButton.IsChecked = false;
            }
            
            var jsonFiles = Directory.GetFiles("./Games", "*.json");
            foreach (var jf in jsonFiles)
            {
                var data = await JsonControl.ReadExeJson(jf);
                if (data.Name == Data.Name)
                {
                    continue;
                }

                MultiplePanel.Children.Add(new CheckBox()
                {
                    Content = data.Name,
                    IsChecked = Data.MultipleLaunch.Contains(data.Name),
                    Tag = data.FilePath
                });
            }
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

   
    }