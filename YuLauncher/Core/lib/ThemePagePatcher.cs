using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Wpf.Ui;
using Wpf.Ui.Appearance;
using WPFLocalizeExtension.Providers;

namespace YuLauncher.Core.lib;


public static class VisualTreeHelperExtensions
{
    public static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
    {
        if (depObj != null)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                if (child != null && child is T)
                {
                    yield return (T)child;
                }

                foreach (T childOfChild in FindVisualChildren<T>(child))
                {
                    yield return childOfChild;
                }
            }
        }
    }
}

public class ThemePagePatcher
{

    struct ThemeControl
    {
        private TextBlock _textBlock;
        private TextBox _textBox;
    }
    
    public static void PatchTheme(Page page)
    {
        var themeControl = new ThemeService().GetTheme();
        switch (themeControl)
        {
            case ApplicationTheme.Dark:
                var textBlocks = VisualTreeHelperExtensions.FindVisualChildren<TextBlock>(page);
                var textBoxes = VisualTreeHelperExtensions.FindVisualChildren<TextBox>(page);
                textBlocks.ToList().ForEach(x => x.Foreground = new SolidColorBrush(Colors.White));
                textBoxes.ToList().ForEach(x => x.Foreground = new SolidColorBrush(Colors.White));
                break;
            case ApplicationTheme.Light:
                var textBlocksLight = VisualTreeHelperExtensions.FindVisualChildren<TextBlock>(page);
                var textBoxesLight = VisualTreeHelperExtensions.FindVisualChildren<TextBox>(page);
                textBlocksLight.ToList().ForEach(x => x.Foreground = new SolidColorBrush(Colors.Black));
                textBoxesLight.ToList().ForEach(x => x.Foreground = new SolidColorBrush(Colors.Black));
                break;
        }
    }
}