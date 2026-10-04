using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Web.WebView2.Core;
using YuLauncher.Core.lib;

namespace YuLauncher.Game;

internal sealed record WebGameNetworkEvent(DateTimeOffset ObservedAtUtc, string DocumentOrigin, string Url,
    string Method, int Status, string ContentType)
{
    internal CoreWebView2WebResourceResponseView? Response { get; set; }
}

internal sealed class WebGameNetworkMonitor : IDisposable
{
    // 非seekable応答streamの読み取りを判定と明示詳細表示で共有する。
    // 最大8応答・各1MiB。JsonDocumentは呼び出し元ごとに所有する。
    private static readonly Dictionary<CoreWebView2WebResourceResponseView, Task<ReadOnlyMemory<byte>>> Reads = new();
    private static readonly List<CoreWebView2WebResourceResponseView> ReadOrder = new();
    private readonly Queue<WeakReference<CoreWebView2WebResourceResponseView>> _ownedReads = new();
    private readonly CoreWebView2 _core;
    private readonly WebGameLoginSettings _settings;
    private readonly Action<WebGameLoginMode> _detected;
    private readonly Action<string> _error;
    private readonly Action<WebGameNetworkEvent>? _observed;
    private readonly WebGameNetworkConditions? _condition;
    private long _generation;
    private string? _navigationOrigin;
    private bool _disposed;
    private TaskCompletionSource<(WebGameNetworkEvent Event, JsonDocument Json)>? _capture;
    private WebGameNetworkRule? _captureRule;
    private string? _captureOrigin;
    internal bool ObservationEnabled { get; set; } = true;

    internal WebGameNetworkMonitor(CoreWebView2 core, WebGameLoginSettings settings, Action<WebGameLoginMode> detected,
        Action<string> reportError, Action<WebGameNetworkEvent>? observed = null)
    {
        _core = core; _settings = settings; _detected = detected; _error = reportError; _observed = observed;
        if (settings.Mode == WebGameLoginMode.Network) _condition = new(settings.NetworkRule!);
        try { core.WebResourceResponseReceived += Received; core.NavigationStarting += Navigating; }
        catch { Dispose(); throw; }
    }
    internal async Task<(WebGameNetworkEvent Event, JsonDocument Json)> CaptureNextAsync(
        WebGameNetworkRule rule, string origin, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (_disposed || !ObservationEnabled || CurrentOrigin() != origin)
            throw new WebGameResponseException("WebGameEventExpired");
        _capture?.TrySetException(new WebGameResponseException("WebGameEventExpired"));
        var completion = new TaskCompletionSource<(WebGameNetworkEvent, JsonDocument)>(TaskCreationOptions.RunContinuationsAsynchronously);
        _captureRule = WebGameNetworkConditions.Normalize(rule with { JsonPointer = null, ExpectedJson = null });
        _captureOrigin = origin; _capture = completion;
        using var registration = cancellationToken.Register(() => completion.TrySetCanceled(cancellationToken));
        try { return await completion.Task; }
        finally
        {
            if (ReferenceEquals(_capture, completion)) { _capture = null; _captureRule = null; _captureOrigin = null; }
        }
    }
    private void OwnResponse(CoreWebView2WebResourceResponseView response)
    {
        _ownedReads.Enqueue(new(response));
        while (_ownedReads.Count > 8)
            if (_ownedReads.Dequeue().TryGetTarget(out var old)) ReleaseResponse(old);
    }
    private void Navigating(object? sender, CoreWebView2NavigationStartingEventArgs e)
    {
        _generation++;
        try { _navigationOrigin = WebGameLoginRepository.GetOrigin(e.Uri); }
        catch (ArgumentException) { _navigationOrigin = null; }
        if (_capture is not null && _captureOrigin != _navigationOrigin)
        {
            _capture.TrySetException(new WebGameResponseException("WebGameEventExpired"));
            _capture = null; _captureRule = null; _captureOrigin = null;
        }
    }
    private string? CurrentOrigin()
    { try { return WebGameLoginRepository.GetOrigin(_core.Source); } catch (ArgumentException) { return null; } }

    private async void Received(object? sender, CoreWebView2WebResourceResponseReceivedEventArgs e)
    {
        if (_disposed) return;
        try
        {
            string url;
            try { url = WebGameNetworkConditions.NormalizeUrl(e.Request.Uri, true); }
            catch (ArgumentException) { return; }
            var origin = CurrentOrigin() ?? _navigationOrigin;
            if (origin is null) return;
            var method = e.Request.Method.ToUpperInvariant();
            var response = e.Response;
            var status = response.StatusCode;
            var contentType = response.Headers.Contains("Content-Type") ? response.Headers.GetHeader("Content-Type") : "";
            var generation = _generation;
            var capture = _capture;
            Task<JsonDocument>? captureRead = null;
            if (capture is not null && !capture.Task.IsCompleted && ObservationEnabled && origin == _captureOrigin &&
                WebGameNetworkConditions.MatchesMetadata(_captureRule!, url, method, status))
            {
                _capture = null; _captureRule = null; _captureOrigin = null;
                OwnResponse(response);
                // 明示された次の一致応答だけ、受信イベント内で本文取得を開始する。
                captureRead = ReadResponseJsonAsync(response, contentType);
            }
            else capture = null;
            WebGameNetworkEvent? row = capture is not null || ObservationEnabled && _observed is not null ?
                new(DateTimeOffset.UtcNow, origin, url, method, status, contentType) { Response = response } : null;
            if (ObservationEnabled && _observed is not null) _observed(row!);
            if (capture is not null)
            {
                try
                {
                    var json = await captureRead!;
                    if (_disposed || generation != _generation)
                    {
                        json.Dispose(); capture.TrySetException(new WebGameResponseException("WebGameEventExpired"));
                    }
                    else if (!capture.TrySetResult((row!, json))) json.Dispose();
                }
                catch (Exception ex) { capture.TrySetException(ex); }
            }
            if (_condition is null || CurrentOrigin() != _settings.TargetOrigin ||
                !WebGameNetworkConditions.MatchesMetadata(_condition.Rule, url, method, status)) return;
            if (_condition.Rule.JsonPointer is not null)
            {
                if (capture is null) OwnResponse(response);
                using var body = await ReadResponseJsonAsync(response, contentType);
                if (_disposed || generation != _generation || CurrentOrigin() != _settings.TargetOrigin || !_condition.MatchesJson(body.RootElement)) return;
            }
            if (!_disposed && generation == _generation && CurrentOrigin() == _settings.TargetOrigin) _detected(WebGameLoginMode.Network);
        }
        catch (WebGameResponseException ex) { if (!_disposed) _error(LocalizeControl.GetLocalize<string>(ex.ResourceKey)); }
        catch (Exception ex) when (ex is NotImplementedException or NotSupportedException)
        { if (!_disposed) _error(LocalizeControl.GetLocalize<string>("WebGameEventRuntimeUnsupported")); }
        catch (Exception ex) when (ex is COMException or IOException or InvalidOperationException)
        { if (!_disposed) _error(LocalizeControl.GetLocalize<string>("WebGameEventReadFailed")); }
    }

    internal static async Task<JsonDocument> ReadResponseJsonAsync(CoreWebView2WebResourceResponseView? response, string contentType)
    {
        if (response is null) throw new WebGameResponseException("WebGameEventExpired");
        var media = contentType.Split(';')[0].Trim();
        if (!media.Equals("application/json", StringComparison.OrdinalIgnoreCase) && !media.EndsWith("+json", StringComparison.OrdinalIgnoreCase))
            throw new WebGameResponseException("WebGameEventNonJson");
        try
        {
            if (!Reads.TryGetValue(response, out var read))
            {
                read = ReadBytesAsync(response);
                Reads.Add(response, read); ReadOrder.Add(response);
                while (ReadOrder.Count > 8) ReleaseResponse(ReadOrder[0]);
            }
            var bytes = await read;
            return await Task.Run(() => JsonDocument.Parse(bytes, new JsonDocumentOptions { MaxDepth = 32 }));
        }
        catch (JsonException) { throw new WebGameResponseException("WebGameEventInvalidJson"); }
        catch (Exception ex) when (ex is NotImplementedException or NotSupportedException)
        { throw new WebGameResponseException("WebGameEventRuntimeUnsupported"); }
        catch (Exception ex) when (ex is COMException or IOException or InvalidOperationException)
        { throw new WebGameResponseException("WebGameEventReadFailed"); }
    }
    internal static void ReleaseResponse(CoreWebView2WebResourceResponseView response)
    { Reads.Remove(response); ReadOrder.Remove(response); }
    private static async Task<ReadOnlyMemory<byte>> ReadBytesAsync(CoreWebView2WebResourceResponseView response)
    {
        // COM呼び出しはUIスレッド。返されたstreamだけを背景で読む。
        var stream = await response.GetContentAsync();
        if (stream is null) throw new WebGameResponseException("WebGameEventNoBody");
        return await Task.Run<ReadOnlyMemory<byte>>(() =>
        {
            using (stream)
            using (var buffer = new MemoryStream())
            {
                var bytes = new byte[8192];
                const int limit = 1024 * 1024;
                while (buffer.Length <= limit)
                {
                    var count = stream.Read(bytes, 0, (int)Math.Min(bytes.Length, limit + 1 - buffer.Length));
                    if (count == 0) break;
                    buffer.Write(bytes, 0, count);
                }
                if (buffer.Length > limit) throw new WebGameResponseException("WebGameEventTooLarge");
                if (buffer.Length == 0) throw new WebGameResponseException("WebGameEventNoBody");
                return buffer.GetBuffer().AsMemory(0, (int)buffer.Length);
            }
        });
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true; _generation++;
        _capture?.TrySetException(new WebGameResponseException("WebGameEventExpired"));
        _capture = null; _captureRule = null; _captureOrigin = null;
        try { _core.WebResourceResponseReceived -= Received; } catch (Exception) { }
        try { _core.NavigationStarting -= Navigating; } catch (Exception) { }
        while (_ownedReads.Count > 0)
            if (_ownedReads.Dequeue().TryGetTarget(out var response)) ReleaseResponse(response);
        _condition?.Dispose();
    }
}
internal sealed class WebGameResponseException(string resourceKey) : Exception
{
    internal string ResourceKey { get; } = resourceKey;
}
