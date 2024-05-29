﻿using System;
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
        ManualTomlSettings _manualTomlSettings = new();
        TomlControl _tomlControl = new();
        public SettingPage()
        {
            InitializeComponent();
            ControlInitialize();
        }

        private void ControlInitialize()
        {
            TxtResHeight.Text = _manualTomlSettings.GetSettingWindowResolution("./settings.toml", "SettingResolution", "Height");
            TxtResWidth.Text = _manualTomlSettings.GetSettingWindowResolution("./settings.toml", "SettingResolution", "Width");
            MenuTxtResHeight.Text = _manualTomlSettings.GetSettingWindowResolution("./settings.toml", "WindowResolution", "Height");
            MenuTxtResWidth.Text = _manualTomlSettings.GetSettingWindowResolution("./settings.toml", "WindowResolution", "Width");
            GameTxtResHeight.Text = _manualTomlSettings.GetSettingWindowResolution("./settings.toml", "GameResolution", "Height");
            GameTxtResWidth.Text = _manualTomlSettings.GetSettingWindowResolution("./settings.toml", "GameResolution", "Width");
        }
        
        private void WindowResChanged()
        {
            _tomlControl.EditTomlList("./settings.toml", "SettingResolution", "Width", TxtResWidth.Text);
            _tomlControl.EditTomlList("./settings.toml", "SettingResolution", "Height", TxtResHeight.Text);
            _tomlControl.EditTomlList("./settings.toml", "WindowResolution", "Width", MenuTxtResWidth.Text);
            _tomlControl.EditTomlList("./settings.toml", "WindowResolution", "Height", MenuTxtResHeight.Text);
            _tomlControl.EditTomlList("./settings.toml", "GameResolution", "Width", TxtResWidth.Text);
            _tomlControl.EditTomlList("./settings.toml", "GameResolution", "Height", TxtResHeight.Text);
        }

        private void SelectLanguage()
        {
            if (LanguageCombo.SelectedItem.Equals(EngItemCombo))
            {
                        _tomlControl.EditToml("./settings.toml", "Language", "en");
                        LanguageUpdater.UpdateLanguage("en-US");
            }
            else if (LanguageCombo.SelectedItem.Equals(JpnItemCombo))
            {
                        _tomlControl.EditToml("./settings.toml", "Language", "ja");
                        LanguageUpdater.UpdateLanguage("ja-JP");
            }
        }

        private void FullSc_OnLoaded(object sender, RoutedEventArgs e)
        {
            if (_tomlControl.GetTomlString("./settings.toml", "FullScreen") == "true")
            {
                FullSc.IsChecked = true;
            }
            else if (_tomlControl.GetTomlString("./settings.toml", "FullScreen") == "false")
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
            switch (_tomlControl.GetTomlString("./settings.toml", "Language"))
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
