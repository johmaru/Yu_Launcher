using System;
using System.Threading.Tasks;
using System.Windows;
using Wpf.Ui.Controls;
using YuLauncher.Core.lib;

namespace YuLauncher.Game.Window;

public partial class VolumeWindow : FluentWindow
{
    private JsonControl.ApplicationJsonData _data;
    private readonly Action<JsonControl.ApplicationJsonData> _onSave;

    public VolumeWindow(JsonControl.ApplicationJsonData data, Action<JsonControl.ApplicationJsonData> onSave)
    {
        InitializeComponent();
        _data = data;
        _onSave = onSave;
        VolumeSlider.Value = data.Volume ?? 1.0;
    }

    private async void SaveButton_OnClick(object sender, RoutedEventArgs e)
    {
        var volume = Math.Round(VolumeSlider.Value, 1);
        _data = _data with { Volume = volume };
        await JsonControl.CreateExeJson(_data.JsonPath, _data);
        _onSave(_data);
        Close();
    }
}
