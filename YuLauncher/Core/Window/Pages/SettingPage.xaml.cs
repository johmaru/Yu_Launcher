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

                MemoTxtFontSize.Text = TomlControl.GetTomlString(FileControl.Main.Settings, "MemoFontSize");
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
                _tomlControl.EditTomlList(FileControl.Main.Settings, "SettingResolution", "Width", TxtResWidth.Text);
                _tomlControl.EditTomlList(FileControl.Main.Settings, "SettingResolution", "Height", TxtResHeight.Text);

                _tomlControl.EditTomlList(FileControl.Main.Settings, "WindowResolution", "Width", MenuTxtResWidth.Text);
                _tomlControl.EditTomlList(FileControl.Main.Settings, "WindowResolution", "Height", MenuTxtResHeight.Text);

                _tomlControl.EditTomlList(FileControl.Main.Settings, "GameResolution", "Width", GameTxtResWidth.Text);
                _tomlControl.EditTomlList(FileControl.Main.Settings, "GameResolution", "Height", GameTxtResHeight.Text);

                _tomlControl.EditTomlList(FileControl.Main.Settings, "MemoResolution", "Width", MemoTxtResWidth.Text);
                _tomlControl.EditTomlList(FileControl.Main.Settings, "MemoResolution", "Height", MemoTxtResHeight.Text);

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
            if (TomlControl.GetTomlString(FileControl.Main.Settings, "FullScreen") == "true")
            {
                FullSc.IsChecked = true;
            }
            else if (TomlControl.GetTomlString(FileControl.Main.Settings, "FullScreen") == "false")
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
            switch (TomlControl.GetTomlString(FileControl.Main.Settings, "Language"))
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
