using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Wpf.Ui;
using Wpf.Ui.Appearance;
using YuLauncher.Core.lib;

namespace YuLauncher.Core.Window.Pages.Settings;

public partial class VideoGraphics : Page
{
    private readonly ManualTomlSettings _manualTomlSettings = new();
    private readonly TomlControl _tomlControl = new();
    private ThemeService _theme = new();
    private ApplicationTheme    _nowTheme;
    
    public VideoGraphics()
    {
        InitializeComponent();
        ControlInitialize();
       _nowTheme = _theme.GetTheme();
       ThemePagePatcher.PatchTheme(this);
    }
    
     private void ControlInitialize()
        {
            try
            {
                TxtResHeight.Text =
                    _manualTomlSettings.GetSettingWindowResolution(FileControl.Main.Settings, "SettingResolution", "Height");
                TxtResWidth.Text =
                    _manualTomlSettings.GetSettingWindowResolution(FileControl.Main.Settings, "SettingResolution", "Width");

                MenuTxtResHeight.Text =
                    _manualTomlSettings.GetSettingWindowResolution(FileControl.Main.Settings, "WindowResolution", "Height");
                MenuTxtResWidth.Text =
                    _manualTomlSettings.GetSettingWindowResolution(FileControl.Main.Settings, "WindowResolution", "Width");

                GameTxtResHeight.Text =
                    _manualTomlSettings.GetSettingWindowResolution(FileControl.Main.Settings, "GameResolution", "Height");
                GameTxtResWidth.Text =
                    _manualTomlSettings.GetSettingWindowResolution(FileControl.Main.Settings, "GameResolution", "Width");

                MemoTxtResHeight.Text =
                    _manualTomlSettings.GetSettingWindowResolution(FileControl.Main.Settings, "MemoResolution", "Height");
                MemoTxtResWidth.Text =
                    _manualTomlSettings.GetSettingWindowResolution(FileControl.Main.Settings, "MemoResolution", "Width");

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
                _tomlControl.EditToml(FileControl.Main.Settings, "SettingResolution", "Width", TxtResWidth.Text);
                _tomlControl.EditToml(FileControl.Main.Settings, "SettingResolution", "Height", TxtResHeight.Text);

                _tomlControl.EditToml(FileControl.Main.Settings, "WindowResolution", "Width", MenuTxtResWidth.Text);
                _tomlControl.EditToml(FileControl.Main.Settings, "WindowResolution", "Height", MenuTxtResHeight.Text);

                _tomlControl.EditToml(FileControl.Main.Settings, "GameResolution", "Width", GameTxtResWidth.Text);
                _tomlControl.EditToml(FileControl.Main.Settings, "GameResolution", "Height", GameTxtResHeight.Text);

                _tomlControl.EditToml(FileControl.Main.Settings, "MemoResolution", "Width", MemoTxtResWidth.Text);
                _tomlControl.EditToml(FileControl.Main.Settings, "MemoResolution", "Height", MemoTxtResHeight.Text);

                _tomlControl.EditToml(FileControl.Main.Settings, "MemoFontSize", MemoTxtFontSize.Text);
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
                        _tomlControl.EditToml(FileControl.Main.Settings, "Language", "en");
                        LanguageUpdater.UpdateLanguage("en-US");
                        LoggerController.LogInfo("Language Changed to English");
            }
            else if (LanguageCombo.SelectedItem.Equals(JpnItemCombo))
            {
                        _tomlControl.EditToml(FileControl.Main.Settings, "Language", "ja");
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
            _tomlControl.EditToml(FileControl.Main.Settings, "FullScreen", "true");
        }

        private void FullSc_OnUnchecked(object sender, RoutedEventArgs e)
        {
            _tomlControl.EditToml(FileControl.Main.Settings, "FullScreen", "false");
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