using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using YuLauncher.Core.lib;
using YuLauncher.Properties;

namespace YuLauncher.Core.Window.Pages
{
    /// <summary>
    /// SettingPage.xaml の相互作用ロジック
    /// </summary>
    public partial class SettingPage : Page
    {
        private readonly ManualTomlSettings _manualTomlSettings = new();
        private readonly TomlControl _tomlControl = new();
        public SettingPage()
        {
            InitializeComponent();
            ControlInitialize();
            LoggerController.LogInfo("SettingPage Initialized");
        }

        private void ControlInitialize()
        {
            TxtResHeight.Text = _manualTomlSettings.GetSettingWindowResolution("./settings.toml", "SettingResolution", "Height");
            TxtResWidth.Text = _manualTomlSettings.GetSettingWindowResolution("./settings.toml", "SettingResolution", "Width");
            
            MenuTxtResHeight.Text = _manualTomlSettings.GetSettingWindowResolution("./settings.toml", "WindowResolution", "Height");
            MenuTxtResWidth.Text = _manualTomlSettings.GetSettingWindowResolution("./settings.toml", "WindowResolution", "Width");
            
            GameTxtResHeight.Text = _manualTomlSettings.GetSettingWindowResolution("./settings.toml", "GameResolution", "Height");
            GameTxtResWidth.Text = _manualTomlSettings.GetSettingWindowResolution("./settings.toml", "GameResolution", "Width");
            
            MemoTxtResHeight.Text = _manualTomlSettings.GetSettingWindowResolution("./settings.toml", "MemoResolution", "Height");
            MemoTxtResWidth.Text = _manualTomlSettings.GetSettingWindowResolution("./settings.toml", "MemoResolution", "Width");
            
            MemoTxtFontSize.Text = TomlControl.GetTomlString("./settings.toml", "MemoFontSize");
        }
        
        private void WindowResChanged()
        {
            _tomlControl.EditTomlList("./settings.toml", "SettingResolution", "Width", TxtResWidth.Text);
            _tomlControl.EditTomlList("./settings.toml", "SettingResolution", "Height", TxtResHeight.Text);
            
            _tomlControl.EditTomlList("./settings.toml", "WindowResolution", "Width", MenuTxtResWidth.Text);
            _tomlControl.EditTomlList("./settings.toml", "WindowResolution", "Height", MenuTxtResHeight.Text);
            
            _tomlControl.EditTomlList("./settings.toml", "GameResolution", "Width", TxtResWidth.Text);
            _tomlControl.EditTomlList("./settings.toml", "GameResolution", "Height", TxtResHeight.Text);
            
            _tomlControl.EditTomlList("./settings.toml", "MemoResolution", "Width", TxtResWidth.Text);
            _tomlControl.EditTomlList("./settings.toml", "MemoResolution", "Height", TxtResHeight.Text);
            
            _tomlControl.EditToml("./settings.toml", "MemoFontSize", MemoTxtFontSize.Text);
        }

        private void SelectLanguage()
        {
            if (LanguageCombo.SelectedItem.Equals(EngItemCombo))
            {
                        _tomlControl.EditToml("./settings.toml", "Language", "en");
                        LanguageUpdater.UpdateLanguage("en-US");
                        LoggerController.LogInfo("Language Changed to English");
            }
            else if (LanguageCombo.SelectedItem.Equals(JpnItemCombo))
            {
                        _tomlControl.EditToml("./settings.toml", "Language", "ja");
                        LanguageUpdater.UpdateLanguage("ja-JP");
                        LoggerController.LogInfo("Language Changed to Japanese");
            }
        }

        private void FullSc_OnLoaded(object sender, RoutedEventArgs e)
        {
            if (TomlControl.GetTomlString("./settings.toml", "FullScreen") == "true")
            {
                FullSc.IsChecked = true;
            }
            else if (TomlControl.GetTomlString("./settings.toml", "FullScreen") == "false")
            {
                FullSc.IsChecked = false;
            }
        }

        private void FullSc_OnChecked(object sender, RoutedEventArgs e)
        {
            _tomlControl.EditToml("./settings.toml", "FullScreen", "true");
        }

        private void FullSc_OnUnchecked(object sender, RoutedEventArgs e)
        {
            _tomlControl.EditToml("./settings.toml", "FullScreen", "false");
        }

        private void SaveBtn_OnClick(object sender, RoutedEventArgs e)
        {
            SelectLanguage();
            WindowResChanged();
        }

        private void FrameworkElement_OnLoaded(object sender, RoutedEventArgs e)
        {
            switch (TomlControl.GetTomlString("./settings.toml", "Language"))
            {
                case "en":
                    LanguageCombo.SelectedItem = EngItemCombo;
                    break;
                case "ja":
                    LanguageCombo.SelectedItem = JpnItemCombo;
                    break;
            }
        }
    }
    }
