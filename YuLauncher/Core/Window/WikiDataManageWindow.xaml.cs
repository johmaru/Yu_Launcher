using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;
using YuLauncher.Core.lib;
using Button = System.Windows.Controls.Button;
using MessageBox = System.Windows.MessageBox;
using TextBox = Wpf.Ui.Controls.TextBox;

namespace YuLauncher.Core.Window;

public partial class WikiDataManageWindow : FluentWindow
{
    private  JsonControl.ApplicationJsonData Data { get; set; }
    public WikiDataManageWindow(JsonControl.ApplicationJsonData data)
    {
        InitializeComponent();
        ApplicationThemeManager.Apply(this);
        
        Data = data;
        
        Initialize();
    }

    private void Initialize()
    {
        if (Data.WikiData != null){
            Dictionary<string,string> wikiData = Data.WikiData;
            
            foreach (var (key, value) in wikiData)
            {
                StackPanel stackPanel = new()
                {
                    Orientation = Orientation.Horizontal,
                    Margin = new Thickness(5),
                };
                
                Button deleteButton = new()
                {
                    Content = LocalizeControl.GetLocalize<string>("DeleteGame"),
                    Tag = key,
                    Margin = new Thickness(5),
                };
                
                TextBox keyTextBox = new()
                {
                    Text = key,
                    Margin = new Thickness(5),
                };
                TextBox valueTextBox = new()
                {
                    Text = value,
                    Margin = new Thickness(5),
                };
                deleteButton.Click += async (_, _) =>
                {
                    var tagString = deleteButton.Tag as string;
                    if (string.IsNullOrEmpty(tagString))
                    {
                        return;
                    }

                    wikiData.Remove(tagString);
                    Data = Data with { WikiData = wikiData };
                    await JsonControl.CreateExeJson(Data.JsonPath, Data);
                    await RefreshContents();
                    await RefreshControl();
                };
                stackPanel.Children.Add(deleteButton);
                stackPanel.Children.Add(keyTextBox);
                stackPanel.Children.Add(valueTextBox);
                WrapPanel.Children.Add(stackPanel);
            }
        }
    }

    private ValueTask RefreshControl()
    {
        WrapPanel.Children.Clear();
        if (Data.WikiData != null)
        {
            Dictionary<string,string> wikiData = Data.WikiData;
            
            foreach (var (key, value) in wikiData)
            {
                StackPanel stackPanel = new()
                {
                    Orientation = Orientation.Horizontal,
                    Margin = new Thickness(5),
                };
                
                Button deleteButton = new()
                {
                    Content =  LocalizeControl.GetLocalize<string>("DeleteGame"),
                    Tag = key,
                    Margin = new Thickness(5),
                };
                
                deleteButton.Click += async (_, _) =>
                {
                    var tagString = deleteButton.Tag as string;
                    if (string.IsNullOrEmpty(tagString))
                    {
                        return;
                    }

                    wikiData.Remove(tagString);
                    Data = Data with { WikiData = wikiData };
                    await JsonControl.CreateExeJson(Data.JsonPath, Data);
                    await RefreshContents();
                    await RefreshControl();
                };
                
                TextBox keyTextBox = new()
                {
                    Text = key,
                    Margin = new Thickness(5),
                };
                TextBox valueTextBox = new()
                {
                    Text = value,
                    Margin = new Thickness(5),
                };
                stackPanel.Children.Add(deleteButton);
                stackPanel.Children.Add(keyTextBox);
                stackPanel.Children.Add(valueTextBox);
                WrapPanel.Children.Add(stackPanel);
            }
        }
        return ValueTask.CompletedTask;
    }
    
    private async ValueTask RefreshContents()
    {
       
            Data = await JsonControl.ReadExeJson(Data.JsonPath);
    }


    private void AddButton_OnClick(object sender, RoutedEventArgs e)
    {
        StackPanel stackPanel = new()
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(5),
        };
        
        Button deleteButton = new()
        {
            Content = "Delete",
            Margin = new Thickness(5),
        };
        
        deleteButton.Click += (_, _) =>
        {
            WrapPanel.Children.Remove(stackPanel);
        };
        
        TextBox keyTextBox = new()
        {
            Text = "Key",
            Margin = new Thickness(5),
        };
        
        TextBox valueTextBox = new()
        {
            Text = "Value",
            Margin = new Thickness(5),
        };
        stackPanel.Children.Add(deleteButton);
        stackPanel.Children.Add(keyTextBox);
        stackPanel.Children.Add(valueTextBox);
        WrapPanel.Children.Add(stackPanel);
    }

    private async void SaveButton_OnClick(object sender, RoutedEventArgs e)
    {
        var stackPanels = WrapPanel.Children.OfType<StackPanel>().ToList();
        
        Dictionary<string,string> distinct = new();
        foreach (var stackPanel in stackPanels)
        {
            var key = stackPanel.Children.OfType<TextBox>().First().Text;
            var value = stackPanel.Children.OfType<TextBox>().Last().Text;
            try
            {
                distinct.Add(key,value);
            }
            catch (ArgumentException)
            {
                MessageBox.Show(LocalizeControl.GetLocalize<string>("SimpleDuplicateKey"));
                return;
            }
        }

        await JsonControl.CreateExeJson( Data.JsonPath, Data with { WikiData = distinct });

        await RefreshContents();

          await RefreshControl();
          
          MessageBox.Show(LocalizeControl.GetLocalize<string>("SimpleComplete"));
    }

    private void ExitButton_OnClick(object sender, RoutedEventArgs e)
    {
        Close();
    }

}
