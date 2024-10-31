using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Wpf.Ui;
using YuLauncher.Core.lib;

namespace YuLauncher.Core.Window.Pages.Settings;

public partial class VideoGraphics : Page
{
    private ThemeService _theme = new();

    public VideoGraphics()
    {
        InitializeComponent();
        ControlInitialize();
       _theme.GetTheme();
       ThemePagePatcher.PatchTheme(this);
    }
    
     private void ControlInitialize()
        {
            try
            {
                TxtResHeight.Text =
                    ManualTomlSettings.GetSettingWindowResolution(FileControl.Main.Settings, "SettingResolution", "Height");
                TxtResWidth.Text =
                    ManualTomlSettings.GetSettingWindowResolution(FileControl.Main.Settings, "SettingResolution", "Width");

                MenuTxtResHeight.Text =
                    ManualTomlSettings.GetSettingWindowResolution(FileControl.Main.Settings, "WindowResolution", "Height");
                MenuTxtResWidth.Text =
                    ManualTomlSettings.GetSettingWindowResolution(FileControl.Main.Settings, "WindowResolution", "Width");

                GameTxtResHeight.Text =
                    ManualTomlSettings.GetSettingWindowResolution(FileControl.Main.Settings, "GameResolution", "Height");
                GameTxtResWidth.Text =
                    ManualTomlSettings.GetSettingWindowResolution(FileControl.Main.Settings, "GameResolution", "Width");

                MemoTxtResHeight.Text =
                    ManualTomlSettings.GetSettingWindowResolution(FileControl.Main.Settings, "MemoResolution", "Height");
                MemoTxtResWidth.Text =
                    ManualTomlSettings.GetSettingWindowResolution(FileControl.Main.Settings, "MemoResolution", "Width");

                MemoTxtFontSize.Text = TomlControl.GetToml(FileControl.Main.Settings, "MemoFontSize");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
        
        private void WindowResChanged()
        {
            try
            {
                TomlControl.EditToml(FileControl.Main.Settings, "SettingResolution", "Width", TxtResWidth.Text);
                TomlControl.EditToml(FileControl.Main.Settings, "SettingResolution", "Height", TxtResHeight.Text);

                TomlControl.EditToml(FileControl.Main.Settings, "WindowResolution", "Width", MenuTxtResWidth.Text);
                TomlControl.EditToml(FileControl.Main.Settings, "WindowResolution", "Height", MenuTxtResHeight.Text);

                TomlControl.EditToml(FileControl.Main.Settings, "GameResolution", "Width", GameTxtResWidth.Text);
                TomlControl.EditToml(FileControl.Main.Settings, "GameResolution", "Height", GameTxtResHeight.Text);

                TomlControl.EditToml(FileControl.Main.Settings, "MemoResolution", "Width", MemoTxtResWidth.Text);
                TomlControl.EditToml(FileControl.Main.Settings, "MemoResolution", "Height", MemoTxtResHeight.Text);

                TomlControl.EditToml(FileControl.Main.Settings, "MemoFontSize", MemoTxtFontSize.Text);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        private void SelectLanguage()
        {
            if (LanguageCombo.SelectedItem.Equals(EngItemCombo))
            {
                        TomlControl.EditToml(FileControl.Main.Settings, "Language", "en");
                        LanguageUpdater.UpdateLanguage("en-US");
                        LoggerController.LogInfo("Language Changed to English");
            }
            else if (LanguageCombo.SelectedItem.Equals(JpnItemCombo))
            {
                        TomlControl.EditToml(FileControl.Main.Settings, "Language", "ja");
                        LanguageUpdater.UpdateLanguage("ja-JP");
                        LoggerController.LogInfo("Language Changed to Japanese");
            }
        }

        private void FullSc_OnLoaded(object sender, RoutedEventArgs e)
        {
            if (TomlControl.GetToml(FileControl.Main.Settings, "FullScreen") == "true")
            {
                FullSc.IsChecked = true;
            }
            else if (TomlControl.GetToml(FileControl.Main.Settings, "FullScreen") == "false")
            {
                FullSc.IsChecked = false;
            }
        }

        private void FullSc_OnChecked(object sender, RoutedEventArgs e)
        {
            TomlControl.EditToml(FileControl.Main.Settings, "FullScreen", "true");
        }

        private void FullSc_OnUnchecked(object sender, RoutedEventArgs e)
        {
            TomlControl.EditToml(FileControl.Main.Settings, "FullScreen", "false");
        }

        private void SaveBtn_OnClick(object sender, RoutedEventArgs e)
        {
            SelectLanguage();
            WindowResChanged();
        }

        private void FrameworkElement_OnLoaded(object sender, RoutedEventArgs e)
        {
            LanguageCombo.SelectedItem = TomlControl.GetToml(FileControl.Main.Settings, "Language") switch
            {
                "en" => EngItemCombo,
                "ja" => JpnItemCombo,
                _ => LanguageCombo.SelectedItem
            };
        }

        private void VideoGraphics_OnLoaded(object sender, RoutedEventArgs e)
        {
           ThemePagePatcher.PatchTheme(this);
        }
}