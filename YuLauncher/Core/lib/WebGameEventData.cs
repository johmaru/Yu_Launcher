using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace YuLauncher.Core.lib;

internal sealed record WebGameNetworkRule(string Url, string Method, int Status,
    string? JsonPointer = null, string? ExpectedJson = null);
internal sealed record WebGameDomEvent(DateTimeOffset ObservedAtUtc, string SourceOrigin, string EventName,
    string TargetKind, string ListenerKind, string? Selector, string? DetailJson, string DetailState);
internal sealed record WebGameDomHook(string SourceOrigin, string EventName, string TargetKind, string ListenerKind, string? Selector,
    string? JsonPointer = null, string? ExpectedJson = null);

internal static class WebGameEventJson
{
    private static readonly string[] SensitiveWords = { "password", "passwd", "secret", "token", "cookie", "authorization", "session", "email", "user", "birthday" };
    internal static string[] ParsePointer(string pointer)
    {
        if (pointer.Length > 4096 || (pointer.Length != 0 && pointer[0] != '/'))
            throw new ArgumentException("Invalid JSON Pointer.");
        if (pointer.Length == 0) return Array.Empty<string>();
        var segments = pointer[1..].Split('/');
        for (var n = 0; n < segments.Length; n++)
        {
            var segment = segments[n];
            for (var i = 0; i < segment.Length; i++)
                if (segment[i] == '~' && (++i == segment.Length || (segment[i] != '0' && segment[i] != '1')))
                    throw new ArgumentException("Invalid JSON Pointer escape.");
            segments[n] = segment.Replace("~1", "/").Replace("~0", "~");
        }
        return segments;
    }

    internal static bool TryGetValue(JsonElement root, IReadOnlyList<string> segments, out JsonElement value)
    {
        value = root;
        foreach (var segment in segments)
        {
            if (value.ValueKind == JsonValueKind.Object)
            {
                if (!value.TryGetProperty(segment, out value)) return false;
            }
            else if (value.ValueKind == JsonValueKind.Array)
            {
                if (segment.Length == 0 || (segment.Length > 1 && segment[0] == '0') ||
                    !int.TryParse(segment, NumberStyles.None, CultureInfo.InvariantCulture, out var index) ||
                    index < 0 || index >= value.GetArrayLength()) return false;
                value = value[index];
            }
            else return false;
        }
        return true;
    }

    internal static bool IsSensitiveKey(string key)
    {
        foreach (var word in SensitiveWords)
            if (key.Contains(word, StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    internal static bool IsReserved(JsonElement value) => value.ValueKind == JsonValueKind.String &&
        value.GetString() is "[redacted]" or "[unavailable]" or "[truncated]";
    internal static string Redact(JsonElement root)
    {
        using var stream = new System.IO.MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            void Copy(JsonElement value)
            {
                if (value.ValueKind == JsonValueKind.Object)
                {
                    writer.WriteStartObject();
                    foreach (var p in value.EnumerateObject())
                    {
                        writer.WritePropertyName(p.Name);
                        if (IsSensitiveKey(p.Name)) writer.WriteStringValue("[redacted]");
                        else Copy(p.Value);
                    }
                    writer.WriteEndObject();
                }
                else if (value.ValueKind == JsonValueKind.Array)
                {
                    writer.WriteStartArray();
                    foreach (var item in value.EnumerateArray()) Copy(item);
                    writer.WriteEndArray();
                }
                else value.WriteTo(writer);
            }
            Copy(root);
        }
        return Encoding.UTF8.GetString(stream.GetBuffer(), 0, (int)stream.Length);
    }
}

internal sealed class WebGameNetworkConditions : IDisposable
{
    private static readonly Regex MethodToken = new("\\A[!#$%&'*+.^_`|~0-9A-Za-z-]+\\z", RegexOptions.CultureInvariant);
    private readonly string[]? _segments;
    private readonly JsonDocument? _expected;
    internal WebGameNetworkRule Rule { get; }
    internal WebGameNetworkConditions(WebGameNetworkRule rule)
    {
        Rule = Normalize(rule);
        if (Rule.JsonPointer is not null)
        {
            _segments = WebGameEventJson.ParsePointer(Rule.JsonPointer);
            _expected = JsonDocument.Parse(Rule.ExpectedJson!);
        }
    }

    internal static string NormalizeUrl(string url, bool stripPrivateParts = false)
    {
        if (url.Length > 8192 || !Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            uri.Scheme is not ("http" or "https") || uri.UserInfo.Length != 0 ||
            (!stripPrivateParts && (uri.Query.Length != 0 || uri.Fragment.Length != 0)))
            throw new ArgumentException("Invalid network URL.");
        return uri.GetLeftPart(UriPartial.Path);
    }

    internal static WebGameNetworkRule Normalize(WebGameNetworkRule rule)
    {
        var url = NormalizeUrl(rule.Url);
        if (rule.Method.Length is 0 or > 64 || !MethodToken.IsMatch(rule.Method) || rule.Status is < 100 or > 599)
            throw new ArgumentException("Invalid network metadata.");
        if ((rule.JsonPointer is null) != (rule.ExpectedJson is null)) throw new ArgumentException("Incomplete JSON condition.");
        if (rule.JsonPointer is not null)
        {
            WebGameEventJson.ParsePointer(rule.JsonPointer);
            if (Encoding.UTF8.GetByteCount(rule.ExpectedJson!) > 2048) throw new ArgumentException("JSON value too large.");
            try
            {
                using var expected = JsonDocument.Parse(rule.ExpectedJson!);
                if (expected.RootElement.ValueKind is JsonValueKind.Object or JsonValueKind.Array)
                    throw new ArgumentException("A scalar JSON value is required.");
            }
            catch (JsonException) { throw new ArgumentException("Invalid JSON value."); }
        }
        return rule with { Url = url, Method = rule.Method.ToUpperInvariant() };
    }

    internal static bool MatchesMetadata(WebGameNetworkRule rule, string url, string method, int status)
    {
        try { return rule.Status == status && string.Equals(rule.Method, method, StringComparison.OrdinalIgnoreCase) &&
            rule.Url == NormalizeUrl(url, true); }
        catch (ArgumentException) { return false; }
    }
    internal bool MatchesJson(JsonElement root) => _segments is null ||
        (WebGameEventJson.TryGetValue(root, _segments, out var value) && JsonElement.DeepEquals(value, _expected!.RootElement));
    public void Dispose() => _expected?.Dispose();
}
