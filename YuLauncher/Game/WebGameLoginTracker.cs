using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Web.WebView2.Core;
using YuLauncher.Core.lib;

namespace YuLauncher.Game;

internal sealed class WebGameLoginSession
{
    private readonly long _gameId;
    private readonly string _sessionId = Guid.NewGuid().ToString("N");
    private bool _recorded;
    internal WebGameLoginSettings Settings { get; }
    internal WebGameLoginSession(long gameId, WebGameLoginSettings settings) { _gameId = gameId; Settings = settings; }
    internal bool TryRecord(WebGameLoginMode method)
    {
        if (_recorded) return true;
        return _recorded = WebGameLoginRepository.RecordLogin(_gameId, _sessionId, method, DateTimeOffset.UtcNow);
    }
}

internal sealed class WebGameLoginTracker : IDisposable
{
    private readonly CoreWebView2 _core;
    private readonly WebGameLoginSettings _settings;
    private readonly Action<WebGameLoginMode> _detected;
    private readonly Action<string> _reportError;
    private readonly string _token = Guid.NewGuid().ToString("N");
    private readonly HashSet<CoreWebView2Frame> _frames = new();
    private string? _scriptId;
    private readonly Action<WebGameNetworkEvent>? _networkObserved;
    private WebGameNetworkMonitor? _network;
    private readonly Action<WebGameDomEvent>? _domObserved;
    private string _eventToken = Guid.NewGuid().ToString("N");
    private string? _observerId;
    private readonly System.Threading.SemaphoreSlim _observationGate = new(1, 1);
    private bool _observationEnabled = true, _captureDetail;
    private bool _disposed, _isPicking;
    internal event Action<string, string>? ElementPicked;
    internal event Action? PickCancelled;
    internal WebGameLoginTracker(CoreWebView2 core, WebGameLoginSettings settings,
        Action<WebGameLoginMode> detected, Action<string> reportError,
        Action<WebGameNetworkEvent>? networkObserved = null, Action<WebGameDomEvent>? domObserved = null)
    { _core = core; _settings = settings; _detected = detected; _reportError = reportError; _networkObserved = networkObserved; _domObserved = domObserved; }

    private string Script
    {
        get
        {
            using var stream = typeof(WebGameLoginTracker).Assembly.GetManifestResourceStream("YuLauncher.Game.WebGameLogin.js")
                ?? throw new InvalidOperationException("Login script resource missing.");
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd() + "(" + JsonSerializer.Serialize(new {
                mode = _settings.Mode.ToString(), targetOrigin = _settings.TargetOrigin,
                successUrl = _settings.SuccessUrl, successSelector = _settings.SuccessSelector,
                javascript = _settings.JavaScript, token = _token }) + ");";
        }
    }

    internal async Task InitializeAsync(IReadOnlyList<CoreWebView2Frame>? existingFrames = null)
    {
        _core.WebMessageReceived += MessageReceived;
        _core.FrameCreated += FrameCreated;
        _core.NavigationCompleted += NavigationCompleted;
        _core.NavigationStarting += NavigationStarting;
        if (_settings.Mode == WebGameLoginMode.Network || _networkObserved is not null)
        {
            try { _network = new(_core, _settings, _detected, _reportError, _networkObserved); }
            catch (Exception) { Error(LocalizeControl.GetLocalize<string>("WebGameEventRuntimeUnsupported")); }
        }
        if (existingFrames is not null) foreach (var frame in existingFrames) AddFrame(frame);
        var scriptId = await _core.AddScriptToExecuteOnDocumentCreatedAsync(Script);
        if (_disposed) { _core.RemoveScriptToExecuteOnDocumentCreated(scriptId); return; }
        _scriptId = scriptId;
        if (_domObserved is not null)
        {
            var observerId = await _core.AddScriptToExecuteOnDocumentCreatedAsync(ObserverScript);
            if (_disposed) { _core.RemoveScriptToExecuteOnDocumentCreated(observerId); return; }
            _observerId = observerId;
        }
    }
    internal IReadOnlyList<CoreWebView2Frame> GetLiveFrames() => _frames.Where(f => f.IsDestroyed() == 0).ToArray();
    private void FrameCreated(object? sender, CoreWebView2FrameCreatedEventArgs e)
    {
        try { AddFrame(e.Frame); }
        catch (Exception ex) { Error("WebView2 frame API: " + ex.GetType().Name + ". Update WebView2 Runtime."); }
    }
    private void AddFrame(CoreWebView2Frame frame)
    {
        if (frame.IsDestroyed() != 0 || !_frames.Add(frame)) return;
        try
        {
            frame.WebMessageReceived += MessageReceived;
            frame.FrameCreated += FrameCreated;
            frame.Destroyed += FrameDestroyed;
            frame.NavigationStarting += NavigationStarting;
            if (_domObserved is not null) frame.NavigationCompleted += FrameNavigationCompleted;
        }
        catch { RemoveFrame(frame); throw; }
    }
    private void RemoveFrame(CoreWebView2Frame frame)
    {
        _frames.Remove(frame);
        // 破棄との競合や旧Runtimeでも、残りの購読解除を続ける。
        void Detach(Action action) { try { action(); } catch (Exception) { } }
        Detach(() => frame.WebMessageReceived -= MessageReceived);
        Detach(() => frame.FrameCreated -= FrameCreated);
        Detach(() => frame.Destroyed -= FrameDestroyed);
        Detach(() => frame.NavigationStarting -= NavigationStarting);
        if (_domObserved is not null) Detach(() => frame.NavigationCompleted -= FrameNavigationCompleted);
    }
    private void FrameDestroyed(object? sender, object e) { if (sender is CoreWebView2Frame f) RemoveFrame(f); }
    private void NavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
    { if (_isPicking) { CancelElementPick(); PickCancelled?.Invoke(); } }
    private async void NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        if (!_disposed && _domObserved is not null) await ExecuteAllAsync(EnsurePreviewScripts);
        if (!_disposed && _settings.Mode == WebGameLoginMode.Connection && e.IsSuccess &&
            e.HttpStatusCode is >= 200 and <= 299 && Origin(_core.Source) == _settings.TargetOrigin)
            _detected(WebGameLoginMode.Connection);
    }
    // NewWindow割り当て時に初回documentへの登録scriptが実行されないRuntime経路を補完する。
    // 登録済みの利用者JavaScriptは再実行しない。
    private string EnsurePreviewScripts =>
        "if(typeof window.__yuLauncherLoginDispose!=='function'){" + Script + "}" +
        "if(typeof window.__yuLauncherEventObserverDispose!=='function'){" + ObserverScript + "}";
    private async void FrameNavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        if (_disposed || _domObserved is null || sender is not CoreWebView2Frame frame || frame.IsDestroyed() != 0) return;
        try { await frame.ExecuteScriptAsync(EnsurePreviewScripts); }
        catch (Exception) { if (!_disposed && frame.IsDestroyed() == 0) Error(LocalizeControl.GetLocalize<string>("WebGameEventRuntimeUnsupported")); }
    }
    private static string? Origin(string source)
    { try { return WebGameLoginRepository.GetOrigin(source); } catch (ArgumentException) { return null; } }
    private string ObserverScript
    {
        get
        {
            using var stream = typeof(WebGameLoginTracker).Assembly.GetManifestResourceStream("YuLauncher.Game.WebGameEventObserver.js")!;
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd() + "(" + JsonSerializer.Serialize(new { token = _eventToken, enabled = _observationEnabled, captureDetail = _captureDetail }) + ");";
        }
    }
    internal Task<(WebGameNetworkEvent Event, JsonDocument Json)> CaptureNextNetworkAsync(
        WebGameNetworkRule rule, string origin, System.Threading.CancellationToken cancellationToken) =>
        _network is not null ? _network.CaptureNextAsync(rule, origin, cancellationToken) :
            Task.FromException<(WebGameNetworkEvent, JsonDocument)>(new WebGameResponseException("WebGameEventRuntimeUnsupported"));
    internal async Task SetObservationOptionsAsync(bool enabled, bool captureDetail)
    {
        await _observationGate.WaitAsync();
        try
        {
            if (_disposed) return;
            _observationEnabled = enabled; _captureDetail = enabled && captureDetail;
            if (_network is not null) _network.ObservationEnabled = enabled;
            if (_domObserved is null) return;
            _eventToken = Guid.NewGuid().ToString("N");
            await ExecuteAllAsync("window.__yuLauncherEventObserverDispose?.();");
            if (_disposed) return;
            if (_observerId is not null) _core.RemoveScriptToExecuteOnDocumentCreated(_observerId);
            var id = await _core.AddScriptToExecuteOnDocumentCreatedAsync(ObserverScript);
            if (_disposed) { _core.RemoveScriptToExecuteOnDocumentCreated(id); return; }
            _observerId = id;
            await ExecuteAllAsync(ObserverScript);
        }
        finally { _observationGate.Release(); }
    }
    private void ObserveDom(JsonElement root, string source)
    {
        if (!_observationEnabled || _domObserved is null || root.ValueKind != JsonValueKind.Object) return;
        string? Text(string key) => root.TryGetProperty(key, out var p) && p.ValueKind == JsonValueKind.String ? p.GetString() : null;
        if (Text("token") != _eventToken || Text("type") != "dom") return;
        var origin = Origin(source); var name = Text("eventName"); var kind = Text("targetKind"); var selector = Text("selector");
        var listenerKind = Text("listenerKind");
        var state = Text("detailState"); var detail = Text("detailJson");
        if (origin is null || name is null || name.Length is 0 or > 256 ||
            kind is not ("window" or "document" or "element" or "unsupported") || selector?.Length > 4096 ||
            listenerKind is not ("window" or "document" or "element" or "unsupported") ||
            (listenerKind == "element" && kind != "element") ||
            (kind == "element" && string.IsNullOrWhiteSpace(selector)) || state is not ("notCaptured" or "available" or "unavailable")) return;
        if (!_captureDetail) { state = "notCaptured"; detail = null; }
        if (state == "available")
        {
            if (detail is null || Encoding.UTF8.GetByteCount(detail) > 2048) return;
            using var parsed = JsonDocument.Parse(detail, new JsonDocumentOptions { MaxDepth = 8 });
            detail = WebGameEventJson.Redact(parsed.RootElement);
        }
        else detail = null;
        _domObserved(new(DateTimeOffset.UtcNow, origin, name, kind!, listenerKind!, selector, detail, state!));
    }
    private void MessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        if (_disposed) return;
        try
        {
            var json = e.WebMessageAsJson;
            if (Encoding.UTF8.GetByteCount(json) > 8192) return;
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            string? Value(string key) => root.TryGetProperty(key, out var p) && p.ValueKind == JsonValueKind.String ? p.GetString() : null;
            if (root.ValueKind == JsonValueKind.Object && Value("channel") == "yuLauncher.events") { ObserveDom(root, e.Source); return; }
            if (root.ValueKind != JsonValueKind.Object || Value("channel") != "yuLauncher.login" || Value("token") != _token) return;
            var origin = Origin(e.Source);
            if (origin is null) return;
            var type = Value("type");
            if (_isPicking && type == "picked")
            {
                var selector = Value("selector");
                if (string.IsNullOrWhiteSpace(selector)) return;
                CancelElementPick(); ElementPicked?.Invoke(origin, selector); return;
            }
            if (_isPicking && type == "pickCancelled") { CancelElementPick(); PickCancelled?.Invoke(); return; }
            if (_isPicking && type == "pickerError") { Error(Value("message") ?? "Element selection failed."); return; }
            if (origin != _settings.TargetOrigin) return;
            if (type == "complete" && _settings.Mode is WebGameLoginMode.Url or WebGameLoginMode.Element or WebGameLoginMode.JavaScript) _detected(_settings.Mode);
            else if (type == "error") Error(Value("message") ?? "Login script failed.");
        }
        catch (JsonException) { }
        catch (InvalidOperationException) { }
    }
    private void Error(string message) => _reportError(message.Length > 1024 ? message[..1024] : message);
    private Task ExecuteAllAsync(string script)
    {
        async Task Execute(Func<Task<string>> run, CoreWebView2Frame? frame = null)
        {
            try { await run(); }
            catch (Exception e) { if (!_disposed && (frame is null || frame.IsDestroyed() == 0)) Error(e.GetType().Name); }
        }
        // 全documentへ先に投入し、旧cleanupが新しい監視の後へ遅延しないようにする。
        var tasks = new List<Task> { Execute(() => _core.ExecuteScriptAsync(script)) };
        foreach (var frame in GetLiveFrames()) tasks.Add(Execute(() => frame.ExecuteScriptAsync(script), frame));
        return Task.WhenAll(tasks);
    }
    internal Task ApplyToCurrentDocumentsAsync() => ExecuteAllAsync(_domObserved is null ? Script : Script + ObserverScript);
    internal Task BeginElementPickAsync()
    {
        CancelElementPick(); _isPicking = true;
        return ExecuteAllAsync(PickerScript + "(" + JsonSerializer.Serialize(_token) + ");");
    }
    internal void CancelElementPick()
    {
        _isPicking = false;
        if (!_disposed) _ = ExecuteAllAsync("window.__yuLauncherPickDispose?.();");
    }
    public void Dispose()
    {
        if (_disposed) return;
        CancelElementPick();
        _ = ExecuteAllAsync("window.__yuLauncherLoginDispose?.();window.__yuLauncherEventObserverDispose?.();");
        _disposed = true;
        _network?.Dispose();
        _core.WebMessageReceived -= MessageReceived;
        _core.FrameCreated -= FrameCreated;
        _core.NavigationCompleted -= NavigationCompleted;
        _core.NavigationStarting -= NavigationStarting;
        foreach (var frame in _frames.ToArray()) RemoveFrame(frame);
        if (_scriptId is not null) _core.RemoveScriptToExecuteOnDocumentCreated(_scriptId);
        if (_observerId is not null) _core.RemoveScriptToExecuteOnDocumentCreated(_observerId);
    }
    private const string PickerScript = """
        (function(token) {
            if (!/^https?:$/.test(location.protocol)) return;
            window.__yuLauncherPickDispose?.();
            let current, outline, priority;
            const send = (type, extra={}) => chrome.webview.postMessage({channel:'yuLauncher.login',token,type,...extra});
            const restore = () => { if(current) { current.style.setProperty('outline',outline,priority); current=null; } };
            const hover = e => {
                const element = e.composedPath().find(n => n instanceof Element);
                if(element === current) return;
                restore(); if(!element) return;
                current=element; outline=element.style.getPropertyValue('outline'); priority=element.style.getPropertyPriority('outline');
                element.style.setProperty('outline','2px solid #ff8c00','important');
            };
            const types=['pointerdown','pointerup','mousedown','mouseup','click','dblclick','contextmenu'];
            const cleanup = () => { restore(); document.removeEventListener('pointermove',hover,true); document.removeEventListener('keydown',key,true); for(const t of types) document.removeEventListener(t,block,true); };
            const key = e => { if(e.key==='Escape') {e.preventDefault();e.stopImmediatePropagation();cleanup();send('pickCancelled');} };
            const block = e => {
                e.preventDefault();e.stopImmediatePropagation();
                if(e.type!=='click') return;
                const element=e.composedPath().find(n=>n instanceof Element);
                try {
                    if(!element || element.getRootNode()!==document) throw new Error('Shadow DOM elements cannot be selected.');
                    let selector;
                    if(element.id && document.querySelectorAll('#'+CSS.escape(element.id)).length===1) selector='#'+CSS.escape(element.id);
                    else {
                        let node=element; const parts=[];
                        while(node && node!==document.body) {
                            const tag=node.localName; let index=1;
                            for(let sibling=node.previousElementSibling;sibling;sibling=sibling.previousElementSibling) if(sibling.localName===tag) index++;
                            parts.unshift(tag+':nth-of-type('+index+')');node=node.parentElement;
                        }
                        if(node!==document.body) throw new Error('Select an element inside body.');
                        selector=['body',...parts].join(' > ');
                    }
                    const matches=document.querySelectorAll(selector);
                    if(matches.length!==1 || matches[0]!==element) throw new Error('A unique selector could not be created.');
                    cleanup();send('picked',{selector});
                } catch(error) {send('pickerError',{message:String(error.message).slice(0,1024)});}
            };
            window.__yuLauncherPickDispose=cleanup;
            document.addEventListener('pointermove',hover,true);document.addEventListener('keydown',key,true);
            for(const t of types) document.addEventListener(t,block,true);
        })
        """;
}
