using System;
using System.IO;
using System.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using YuLauncher.Core.lib;

namespace YuLauncher.Core.Window.Pages.XamlCreateGameDialogInterface;

public partial class WebGame : DialogInterface
{
    private Subject<int> _nameChangeSaveClicked = new();
    public IObservable<int> NameChangeSaveClicked => _nameChangeSaveClicked;
    public WebGame(JsonControl.ApplicationJsonData data) : base(data)
    {
       InitializeComponent();
       InterFace.SetNameLabel(NameTextBlock,InterFace.IsDark());
       InterFace.SetNameBox(NameBox,data.Name,InterFace.IsDark());
       
       InterFace.SetNameLabel(UrlTextBlock,InterFace.IsDark());
       InterFace.SetPathBox(UrlBox,data.Url,InterFace.IsDark());
    }

    private async void SaveButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (NameBox.Text != NowName)
        {
            Data = Data with { Name = NameBox.Text };
            await JsonControl.CreateExeJson(Data.JsonPath,Data);
        }
        
        if (UrlBox.Text != NewPath)
        {
            Data = Data with { Url = UrlBox.Text };
            await JsonControl.CreateExeJson(Data.JsonPath,Data with { Url = UrlBox.Text });
        }
        
        var checkBoxTrue = MultiplePanel.Children.OfType<CheckBox>()
            .Where(cb => cb.IsChecked == true);
                
        var checkBoxFalse = MultiplePanel.Children.OfType<CheckBox>()
            .Where(cb => cb.IsChecked == false);
        var data =JsonControl.LoadJson(Data.JsonPath);
                
        foreach (var cb in checkBoxTrue)
        {
            string[] tag = (string[])cb.Tag;
            Console.WriteLine($"Processing true checkbox with tag: {tag[1]}");

            if (!data.MultipleLaunch.Contains(tag[1]))
            {
                data = data with
                {
                    MultipleLaunch = data.MultipleLaunch.Append(tag[1]).ToArray()
                };
                Console.WriteLine($"Added {tag[1]} to MultipleLaunch");
            }
               
        }

        foreach (var cb in checkBoxFalse)
        {
            string[] tag = (string[])cb.Tag;
            Console.WriteLine($"Processing false checkbox with tag: {tag[1]}");

            data = data with
            {
                MultipleLaunch = data.MultipleLaunch.Where(x => x != tag[1]).ToArray()
            };
            Console.WriteLine($"Removed {tag[1]} from MultipleLaunch");
        }
        await JsonControl.CreateExeJson(data.JsonPath, data);
        
        _nameChangeSaveClicked.OnNext(2);
    }

    private async void WebGame_OnInitialized(object? sender, EventArgs e)
    {
        var jsonFiles = Directory.GetFiles("./Games", "*.json");
        foreach (var jf in jsonFiles)
        {
            var data = await JsonControl.ReadExeJson(jf);
            if (data.Name == Data.Name)
            {
                continue;
            }
            
            if (MultiplePanel.Children.OfType<CheckBox>().Any(cb => ((string[])cb.Tag)[1] == data.Name))
            {
                continue;
            }

            TextBlock text = new TextBlock()
            {
                Text = data.Name,
                FontSize = 20
            };
            string[] tag = [data.FilePath, data.Name];
            var checkBox = new CheckBox()
            {
                Content = text,
                IsChecked = Data.MultipleLaunch.Contains(data.Name),
                Tag = tag
            };

            checkBox.SetBinding(WidthProperty, new Binding("ActualWidth")
            {
                Source = this,
                Mode = BindingMode.OneWay
            });

            MultiplePanel.Children.Add(checkBox);
        }
    }

    private void WebGame_OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        UrlBox.Width = e.NewSize.Width - UrlTextBlock.ActualWidth - 50;
        NameBox.Width = e.NewSize.Width - NameTextBlock.ActualWidth - 50;
    }

    private void GenreManageButton_OnClick(object sender, RoutedEventArgs e)
    {
        var genreManageWindow = new GenreManageWindow(Data);
        genreManageWindow.Show();
    }
}