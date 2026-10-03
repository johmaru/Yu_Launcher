using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using Wpf.Ui.Controls;
using YuLauncher.Core.lib;
using YuLauncher.Game;

namespace YuLauncher.Core.Window;

public partial class WebGameLoginSettingsWindow : FluentWindow
{
    private readonly long _gameId;
    private readonly string _initialUrl;
    private readonly List<PreviewPopup> _popups = new();
    private WebGameLoginSettings _previewSettings;
    private WebGameLoginTracker? _tracker;
    private readonly ObservableCollection<WebGameObservedEvent> _events = new();
    private WebGameEventViewerWindow? _viewer;
    private bool _observe = true, _captureDetail;
    private int _popupNumber;
    private WebGameNetworkRule? _validatedNetwork;
    private bool _ready, _closed, _busy;
    private static string L(string key) => LocalizeControl.GetLocalize<string>(key);
    public WebGameLoginSettingsWindow(long gameId)
    {
        _gameId = gameId;
        using var conn = GameRepository.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT url FROM games WHERE id=$id AND file_extension='WebGame'";
        cmd.Parameters.AddWithValue("$id", gameId);
        _initialUrl = cmd.ExecuteScalar() as string ?? throw new InvalidOperationException("WebGame not found.");
        _previewSettings = WebGameLoginRepository.GetSettings(gameId);
        InitializeComponent();
        _validatedNetwork = _previewSettings.NetworkRule;
        if (_validatedNetwork is { } rule) FillNetwork(rule);
        foreach (var name in new[] { "OpenEventViewerButton", "LoginNetworkUrlBox", "LoginNetworkMethodBox", "LoginNetworkStatusBox", "LoginNetworkUseJsonCheckBox", "LoginNetworkJsonPointerBox", "LoginNetworkExpectedJsonBox" })
            if (FindName(name) is DependencyObject element) System.Windows.Automation.AutomationProperties.SetAutomationId(element, name);
        LoginModeCombo.SelectedIndex = (int)_previewSettings.Mode;
        LoginTargetOriginBox.Text = _previewSettings.TargetOrigin;
        LoginSuccessUrlBox.Text = _previewSettings.SuccessUrl;
        LoginSuccessSelectorBox.Text = _previewSettings.SuccessSelector;
        LoginScriptBox.Text = _previewSettings.JavaScript;
        LoginPreviewUrlBox.Text = _initialUrl;
        UpdateMode();
    }
    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_ready || _closed) return;
        try
        {
            await LoginPreviewWebView.EnsureCoreWebView2Async();
            if (_closed) return;
            var core = LoginPreviewWebView.CoreWebView2;
            core.NewWindowRequested += OpenPopup;
            core.NavigationStarting += (_, _) => { PickLoginElementButton.IsEnabled = false; };
            core.NavigationCompleted += (_, _) => { if (!_closed) PickLoginElementButton.IsEnabled = true; };
            _tracker = CreateTracker(core, _previewSettings);
            await _tracker.InitializeAsync();
            if (!_observe || _captureDetail) await _tracker.SetObservationOptionsAsync(_observe, _captureDetail);
            _ready = true;
            NavigateLoginPreviewButton.IsEnabled = TestLoginRuleButton.IsEnabled = SaveLoginSettingsButton.IsEnabled = UseCurrentUrlButton.IsEnabled = true;
            core.Navigate(_initialUrl);
            Status("WebGameLoginWaiting");
        }
        catch (Exception ex) { Error("WebGameLoginInitializationFailed", ex.GetType().Name); }
    }
    private WebGameLoginTracker CreateTracker(CoreWebView2 core, WebGameLoginSettings settings, string? source = null)
    {
        var tracker = new WebGameLoginTracker(core, settings,
            _ => Status("WebGameLoginTestSuccess"), message => Error("WebGameLoginInitializationFailed", message),
            n => AddObserved(new(source ?? L("WebGameEventMainSource"), n, null)),
            d => AddObserved(new(source ?? L("WebGameEventMainSource"), null, d)));
        tracker.ElementPicked += (origin, selector) => {
            CancelAllPicks(); LoginTargetOriginBox.Text = origin; LoginSuccessSelectorBox.Text = selector; Status("WebGameLoginPicked");
        };
        tracker.PickCancelled += () => { CancelAllPicks(); Status("WebGameLoginPickCancelled"); };
        return tracker;
    }
    private void AddObserved(WebGameObservedEvent row)
    {
        if (_closed || !_observe) { row.Release(); return; }
        _events.Add(row);
        while (_events.Count > 2000) { _events[0].Release(); _events.RemoveAt(0); }
    }
    private void ReleaseSource(string source)
    {
        foreach (var row in _events.Where(r => r.Source == source)) row.Release();
        _viewer?.SourceReleased(source);
    }
    private void FillNetwork(WebGameNetworkRule rule)
    {
        LoginNetworkUrlBox.Text = rule.Url; LoginNetworkMethodBox.Text = rule.Method;
        LoginNetworkStatusBox.Text = rule.Status.ToString(CultureInfo.InvariantCulture);
        LoginNetworkUseJsonCheckBox.IsChecked = rule.JsonPointer is not null;
        LoginNetworkJsonPointerBox.Text = rule.JsonPointer ?? ""; LoginNetworkExpectedJsonBox.Text = rule.ExpectedJson ?? "";
    }
    private async Task SetObservation(bool enabled, bool detail)
    {
        _observe = enabled; _captureDetail = enabled && detail;
        var trackers = _popups.Select(p => p.Tracker).Append(_tracker).Where(t => t is not null).ToArray();
        foreach (var tracker in trackers) await tracker!.SetObservationOptionsAsync(_observe, _captureDetail);
    }
    private async Task<(WebGameObservedEvent Event, System.Text.Json.JsonDocument Json)> CaptureNextResponse(
        WebGameObservedEvent selected, System.Threading.CancellationToken cancellationToken)
    {
        var tracker = _popups.FirstOrDefault(p => p.SourceLabel == selected.Source)?.Tracker ?? _tracker;
        if (tracker is null || selected.Network is not { Response: not null } network)
            throw new WebGameResponseException("WebGameEventExpired");
        var (captured, json) = await tracker.CaptureNextNetworkAsync(
            new(network.Url, network.Method, network.Status), selected.Origin, cancellationToken);
        var row = _events.FirstOrDefault(r => ReferenceEquals(r.Network, captured));
        if (_closed || row?.Network?.Response is null)
        {
            json.Dispose(); throw new WebGameResponseException("WebGameEventExpired");
        }
        return (row, json);
    }
    private async void OpenEventViewer(object sender, RoutedEventArgs e)
    {
        if (_viewer is not null) { _viewer.Activate(); return; }
        _viewer = new(_events, (rule, origin) => {
            LoginModeCombo.SelectedIndex = (int)WebGameLoginMode.Network; LoginTargetOriginBox.Text = origin; FillNetwork(rule);
            Status("WebGameLoginWaiting");
        }, hook => {
            LoginModeCombo.SelectedIndex = (int)WebGameLoginMode.JavaScript; LoginTargetOriginBox.Text = hook.SourceOrigin;
            LoginScriptBox.Text += "\n" + WebGameEventViewerWindow.BuildDomScript(hook) + "\n";
            TrustLoginScriptCheckBox.IsChecked = false; Status("WebGameLoginWaiting");
        }, SetObservation, CaptureNextResponse) { Owner = this };
        _viewer.Closed += async (_, _) => {
            _viewer = null;
            if (!_closed) try { await SetObservation(false, false); } catch (Exception) { Error("WebGameEventRuntimeUnsupported"); }
        };
        _viewer.Show();
        try { await SetObservation(true, false); } catch (Exception) { Error("WebGameEventRuntimeUnsupported"); }
    }
    private void Status(string key) { if (!_closed) LoginPreviewStatusText.Text = L(key); }
    private void Error(string key, string? message = null)
    {
        if (_closed) return;
        LoginSettingsErrorBar.Message = L(key) + (message is null ? "" : "\n" + message[..Math.Min(1024, message.Length)]);
        LoginSettingsErrorBar.IsOpen = true;
        LoggerController.LogError($"WebGame login settings error: gameId={_gameId}, category={key}");
    }
    private void ModeChanged(object sender, SelectionChangedEventArgs e) { if (LoginTargetOriginBox is not null) UpdateMode(); }
    private void UpdateMode()
    {
        var mode = (WebGameLoginMode)LoginModeCombo.SelectedIndex;
        LoginTargetOriginBox.IsReadOnly = mode == WebGameLoginMode.Connection;
        if (mode == WebGameLoginMode.Connection) LoginTargetOriginBox.Text = WebGameLoginRepository.GetOrigin(_initialUrl);
        ConnectionPanel.Visibility = mode == WebGameLoginMode.Connection ? Visibility.Visible : Visibility.Collapsed;
        UrlPanel.Visibility = mode == WebGameLoginMode.Url ? Visibility.Visible : Visibility.Collapsed;
        ElementPanel.Visibility = mode == WebGameLoginMode.Element ? Visibility.Visible : Visibility.Collapsed;
        ScriptPanel.Visibility = mode == WebGameLoginMode.JavaScript ? Visibility.Visible : Visibility.Collapsed;
        NetworkPanel.Visibility = mode == WebGameLoginMode.Network ? Visibility.Visible : Visibility.Collapsed;
    }
    private async Task<WebGameLoginSettings> ReadValidatedAsync()
    {
        var mode = (WebGameLoginMode)LoginModeCombo.SelectedIndex;
        if (mode == WebGameLoginMode.JavaScript && TrustLoginScriptCheckBox.IsChecked != true)
            throw new ArgumentException("Confirm that you trust this script.");
        var originInput = LoginTargetOriginBox.Text.Trim();
        var origin = WebGameLoginRepository.GetOrigin(originInput);
        if (!Uri.TryCreate(originInput, UriKind.Absolute, out var originUri) || originUri.AbsolutePath != "/" ||
            originUri.Query.Length != 0 || originUri.Fragment.Length != 0)
            throw new ArgumentException("Enter an origin without a path, query or fragment.");
        var network = _validatedNetwork;
        if (mode == WebGameLoginMode.Network)
        {
            if (!int.TryParse(LoginNetworkStatusBox.Text, NumberStyles.None, CultureInfo.InvariantCulture, out var status))
                throw new ArgumentException(L("WebGameEventInvalidRule"));
            try { network = WebGameNetworkConditions.Normalize(new(LoginNetworkUrlBox.Text.Trim(), LoginNetworkMethodBox.Text.Trim(), status,
                LoginNetworkUseJsonCheckBox.IsChecked == true ? LoginNetworkJsonPointerBox.Text : null,
                LoginNetworkUseJsonCheckBox.IsChecked == true ? LoginNetworkExpectedJsonBox.Text : null)); }
            catch (ArgumentException) { throw new ArgumentException(L("WebGameEventInvalidRule")); }
        }
        var settings = new WebGameLoginSettings(mode, origin, LoginSuccessUrlBox.Text.Trim(),
            LoginSuccessSelectorBox.Text.Trim(), LoginScriptBox.Text, network);
        WebGameLoginRepository.ValidateSettings(settings);
        string? validation = mode switch
        {
            WebGameLoginMode.Element => "document.querySelector(" + JsonSerializer.Serialize(settings.SuccessSelector) + ");",
            WebGameLoginMode.JavaScript => "new (Object.getPrototypeOf(async function(){}).constructor)('yuLogin'," + JsonSerializer.Serialize(settings.JavaScript) + ");",
            _ => null
        };
        if (validation is not null)
        {
            var result = await LoginPreviewWebView.CoreWebView2.ExecuteScriptAsync("(()=>{try{" + validation + "return null;}catch(e){return String(e.message).slice(0,1024);}})();");
            var error = JsonSerializer.Deserialize<string?>(result);
            if (error is not null) throw new ArgumentException(error);
        }
        _validatedNetwork = network;
        return settings;
    }
    private async Task<WebGameLoginTracker> ReplaceTracker(CoreWebView2 core, WebGameLoginTracker? previous, WebGameLoginSettings settings, string? source = null)
    {
        var frames = previous?.GetLiveFrames();
        previous?.Dispose();
        ReleaseSource(source ?? L("WebGameEventMainSource"));
        var next = CreateTracker(core, settings, source);
        try { await next.InitializeAsync(frames); await next.SetObservationOptionsAsync(_observe, _captureDetail); await next.ApplyToCurrentDocumentsAsync(); return next; }
        catch { next.Dispose(); throw; }
    }
    private async void TestRule(object sender, RoutedEventArgs e)
    {
        if (!_ready || _busy) return;
        _busy = true;
        try
        {
            var settings = await ReadValidatedAsync();
            CancelAllPicks(); LoginSettingsErrorBar.IsOpen = false; Status("WebGameLoginWaiting");
            _previewSettings = settings;
            _tracker = await ReplaceTracker(LoginPreviewWebView.CoreWebView2, _tracker, settings);
            foreach (var popup in _popups.ToArray())
                if (popup.Web.CoreWebView2 is not null) popup.Tracker = await ReplaceTracker(popup.Web.CoreWebView2, popup.Tracker, settings, popup.SourceLabel);
        }
        catch (Exception ex) { Error("WebGameLoginInvalidSettings", ex.Message); }
        finally { _busy = false; }
    }
    private async void SaveSettings(object sender, RoutedEventArgs e)
    {
        if (!_ready || _busy) return;
        _busy = true;
        try { var settings = await ReadValidatedAsync(); WebGameLoginRepository.SaveSettings(_gameId, settings); Close(); }
        catch (Exception ex) { Error("WebGameLoginInvalidSettings", ex.Message); }
        finally { _busy = false; }
    }
    private void CancelSettings(object sender, RoutedEventArgs e) => Close();
    private void NavigatePreview(object sender, RoutedEventArgs e)
    {
        try { WebGameLoginRepository.GetOrigin(LoginPreviewUrlBox.Text.Trim()); CancelAllPicks(); LoginPreviewWebView.CoreWebView2.Navigate(LoginPreviewUrlBox.Text.Trim()); }
        catch (Exception ex) { Error("WebGameLoginInvalidSettings", ex.Message); }
    }
    private void UseCurrentUrl(object sender, RoutedEventArgs e)
    {
        try { var uri = new Uri(LoginPreviewWebView.CoreWebView2.Source); LoginTargetOriginBox.Text = WebGameLoginRepository.GetOrigin(uri.AbsoluteUri); LoginSuccessUrlBox.Text = uri.GetLeftPart(UriPartial.Path); }
        catch (Exception ex) { Error("WebGameLoginInvalidSettings", ex.Message); }
    }
    private async void PickElement(object sender, RoutedEventArgs e)
    {
        if (!_ready || _tracker is null) return;
        Status("WebGameLoginPicking");
        await _tracker.BeginElementPickAsync();
        foreach (var popup in _popups.ToArray()) if (popup.Tracker is not null) await popup.Tracker.BeginElementPickAsync();
    }
    private void CancelAllPicks() { _tracker?.CancelElementPick(); foreach (var popup in _popups) popup.Tracker?.CancelElementPick(); }
    private void CancelPick(object sender, RoutedEventArgs e) { CancelAllPicks(); Status("WebGameLoginPickCancelled"); }
    private void InsertSample(object sender, RoutedEventArgs e)
    {
        var sample = LoginSampleCombo.SelectedIndex switch
        {
            1 => "// ログイン後だけ表示される要素\nyuLogin.whenVisible(\"#user-menu\");\n",
            2 => "// 成功画面のURL（query・fragmentは無視）\nyuLogin.whenUrl(\"https://example.com/game/home\");\n",
            _ => "// 成功イベント（イベント名・条件を対象ゲームに合わせる）\nyuLogin.onEvent(window, \"login-success\", event => event.detail?.success === true);\n"
        };
        var position = LoginScriptBox.CaretIndex;
        LoginScriptBox.Text = LoginScriptBox.Text.Insert(position, sample);
        LoginScriptBox.CaretIndex = position + sample.Length;
        LoginScriptBox.Focus();
    }
    private async void OpenPopup(object? sender, CoreWebView2NewWindowRequestedEventArgs e)
    {
        e.Handled = true;
        using var deferral = e.GetDeferral();
        var popup = new PreviewPopup { Owner = this, SourceLabel = string.Format(L("WebGameEventPopupSource"), ++_popupNumber) };
        _popups.Add(popup);
        popup.Closed += (_, _) => { ReleaseSource(popup.SourceLabel); popup.Tracker?.Dispose(); popup.Web.Dispose(); _popups.Remove(popup); };
        popup.Show();
        try
        {
            await popup.Web.EnsureCoreWebView2Async(LoginPreviewWebView.CoreWebView2.Environment);
            popup.Web.CoreWebView2.NewWindowRequested += OpenPopup;
            popup.Tracker = CreateTracker(popup.Web.CoreWebView2, _previewSettings, popup.SourceLabel);
            await popup.Tracker.InitializeAsync();
            await popup.Tracker.SetObservationOptionsAsync(_observe, _captureDetail);
            e.NewWindow = popup.Web.CoreWebView2;
        }
        catch (Exception ex) { Error("WebGameLoginInitializationFailed", ex.GetType().Name); popup.Close(); }
    }
    private void OnClosed(object? sender, EventArgs e)
    {
        _closed = true;
        _viewer?.Close(); foreach (var row in _events) row.Release(); _events.Clear();
        foreach (var popup in _popups.ToArray()) popup.Close();
        _tracker?.Dispose(); LoginPreviewWebView.Dispose();
    }
    private sealed class PreviewPopup : FluentWindow
    {
        internal WebView2 Web { get; } = new();
        internal WebGameLoginTracker? Tracker { get; set; }
        internal string SourceLabel { get; set; } = "";
        internal PreviewPopup()
        {
            Width = 900; Height = 640; Title = L("WebGameLoginPreview");
            var grid = new Grid(); grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(48) }); grid.RowDefinitions.Add(new RowDefinition());
            grid.Children.Add(new TitleBar { Title = Title, Height = 48 }); Grid.SetRow(Web, 1); grid.Children.Add(Web);
            Content = grid; ExtendsContentIntoTitleBar = true;
        }
    }
}
