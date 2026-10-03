using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Dapper;
using System.Text.Json;

namespace YuLauncher.Core.lib;

internal enum WebGameLoginMode { Connection, Url, Element, JavaScript, Network }
internal sealed record WebGameLoginSettings(WebGameLoginMode Mode, string TargetOrigin,
    string SuccessUrl, string SuccessSelector, string JavaScript, WebGameNetworkRule? NetworkRule = null);
internal sealed record WebGameLoginEntry(long Id, long GameId, DateTimeOffset DetectedAtUtc, WebGameLoginMode Method);
internal sealed record WebGameLoginSummary(DateTimeOffset? LastDetectedAtUtc, WebGameLoginMode? LastMethod,
    bool HasRecordToday, WebGameLoginMode? TodayMethod);

internal static class WebGameLoginRepository
{
    internal static event Action<long>? LoginRecorded;

    internal static string GetOrigin(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) ||
            !string.IsNullOrEmpty(uri.UserInfo))
            throw new ArgumentException("An HTTP(S) URL is required.");
        return uri.GetLeftPart(UriPartial.Authority);
    }

    internal static void ValidateSettings(WebGameLoginSettings settings)
    {
        if (!Enum.IsDefined(settings.Mode) || GetOrigin(settings.TargetOrigin) != settings.TargetOrigin)
            throw new ArgumentException("An exact HTTP(S) origin is required.");
        if (settings.NetworkRule is not null) WebGameNetworkConditions.Normalize(settings.NetworkRule);
        if (settings.Mode == WebGameLoginMode.Network && settings.NetworkRule is null)
            throw new ArgumentException("A network rule is required.");
        switch (settings.Mode)
        {
            case WebGameLoginMode.Url when GetOrigin(settings.SuccessUrl) != settings.TargetOrigin:
                throw new ArgumentException("The success URL must have the target origin.");
            case WebGameLoginMode.Element when string.IsNullOrWhiteSpace(settings.SuccessSelector):
                throw new ArgumentException("A selector is required.");
            case WebGameLoginMode.JavaScript when string.IsNullOrWhiteSpace(settings.JavaScript):
                throw new ArgumentException("JavaScript is required.");
        }
    }

    internal static WebGameLoginSettings GetSettings(long gameId)
    {
        using var conn = GameRepository.CreateConnection();
        var url = conn.QuerySingleOrDefault<string>("SELECT url FROM games WHERE id=@gameId AND file_extension='WebGame'", new { gameId });
        if (url is null) throw new InvalidOperationException("WebGame not found.");
        var row = conn.QuerySingleOrDefault<SettingsRow>("""
            SELECT mode AS Mode, target_origin AS TargetOrigin, success_url AS SuccessUrl,
                success_selector AS SuccessSelector, javascript AS JavaScript, network_rule_json AS NetworkRuleJson
            FROM webgame_login_settings WHERE game_id=@gameId
            """, new { gameId });
        if (row is null) return new(WebGameLoginMode.Connection, GetOrigin(url), "", "", "");
        var mode = ParseMode(row.Mode);
        var settings = new WebGameLoginSettings(mode,
            mode == WebGameLoginMode.Connection ? GetOrigin(url) : row.TargetOrigin,
            row.SuccessUrl, row.SuccessSelector, row.JavaScript,
            row.NetworkRuleJson == "" ? null : JsonSerializer.Deserialize<WebGameNetworkRule>(row.NetworkRuleJson)
                ?? throw new InvalidOperationException("Invalid network rule."));
        ValidateSettings(settings);
        return settings;
    }

    internal static void SaveSettings(long gameId, WebGameLoginSettings settings)
    {
        ValidateSettings(settings);
        using var conn = GameRepository.CreateConnection();
        var affected = conn.Execute("""
            INSERT INTO webgame_login_settings(game_id,mode,target_origin,success_url,success_selector,javascript,network_rule_json)
            SELECT id,@mode,@TargetOrigin,@SuccessUrl,@SuccessSelector,@JavaScript,@networkRuleJson
            FROM games WHERE id=@gameId AND file_extension='WebGame'
            ON CONFLICT(game_id) DO UPDATE SET mode=excluded.mode,target_origin=excluded.target_origin,
                success_url=excluded.success_url,success_selector=excluded.success_selector,javascript=excluded.javascript,
                network_rule_json=excluded.network_rule_json
            """, new { gameId, mode = settings.Mode.ToString(), settings.TargetOrigin, settings.SuccessUrl, settings.SuccessSelector, settings.JavaScript,
                networkRuleJson = settings.NetworkRule is null ? "" : JsonSerializer.Serialize(WebGameNetworkConditions.Normalize(settings.NetworkRule)) });
        if (affected != 1) throw new InvalidOperationException("WebGame not found.");
    }

    internal static bool RecordLogin(long gameId, string sessionId, WebGameLoginMode method, DateTimeOffset detectedAtUtc)
    {
        if (gameId <= 0 || string.IsNullOrWhiteSpace(sessionId) || !Enum.IsDefined(method)) return false;
        try
        {
            using var conn = GameRepository.CreateConnection();
            if (conn.Execute("""
                INSERT INTO webgame_login_history(game_id,session_id,detected_at_utc,method)
                SELECT id,@sessionId,@time,@method FROM games WHERE id=@gameId AND file_extension='WebGame'
                ON CONFLICT(game_id,session_id) DO NOTHING
                """, new { gameId, sessionId, time = Format(detectedAtUtc), method = method.ToString() }) != 1)
                return false;
        }
        catch (Exception e)
        {
            LoggerController.LogError($"WebGame login write failed: gameId={gameId}, category={e.GetType().Name}");
            return false;
        }
        if (LoginRecorded is { } handlers)
            foreach (Action<long> handler in handlers.GetInvocationList())
                try { handler(gameId); }
                catch (Exception e) { LoggerController.LogError($"WebGame login notification failed: gameId={gameId}, category={e.GetType().Name}"); }
        return true;
    }

    internal static List<WebGameLoginEntry> GetHistory(long gameId, int offset = 0, int limit = 50)
    {
        if (offset < 0) throw new ArgumentOutOfRangeException(nameof(offset));
        if (limit <= 0) throw new ArgumentOutOfRangeException(nameof(limit));
        using var conn = GameRepository.CreateConnection();
        return conn.Query<HistoryRow>("""
            SELECT id AS Id,game_id AS GameId,detected_at_utc AS Time,method AS Method
            FROM webgame_login_history WHERE game_id=@gameId
            ORDER BY detected_at_utc DESC,id DESC LIMIT @limit OFFSET @offset
            """, new { gameId, offset, limit }).Select(r => new WebGameLoginEntry(r.Id, r.GameId, ParseTime(r.Time), ParseMode(r.Method))).ToList();
    }

    internal static Dictionary<long, WebGameLoginSummary> GetSummaries(IReadOnlyCollection<long> gameIds,
        DateTimeOffset todayStartUtc, DateTimeOffset tomorrowStartUtc)
    {
        if (gameIds.Count == 0) return new();
        using var conn = GameRepository.CreateConnection();
        return conn.Query<SummaryRow>("""
            WITH ranked AS (
                SELECT game_id,detected_at_utc,method,
                    ROW_NUMBER() OVER(PARTITION BY game_id ORDER BY detected_at_utc DESC,id DESC) AS rank
                FROM webgame_login_history WHERE game_id IN @gameIds
            ), today AS (
                SELECT game_id,method,
                    ROW_NUMBER() OVER(PARTITION BY game_id ORDER BY detected_at_utc DESC,id DESC) AS rank
                FROM webgame_login_history WHERE game_id IN @gameIds
                    AND detected_at_utc>=@start AND detected_at_utc<@end
            )
            SELECT g.id AS GameId,r.detected_at_utc AS Time,r.method AS Method,t.method AS TodayMethod
            FROM games g LEFT JOIN ranked r ON r.game_id=g.id AND r.rank=1
                LEFT JOIN today t ON t.game_id=g.id AND t.rank=1
            WHERE g.id IN @gameIds AND g.file_extension='WebGame'
            """, new { gameIds, start = Format(todayStartUtc), end = Format(tomorrowStartUtc) })
            .ToDictionary(r => r.GameId, r => new WebGameLoginSummary(r.Time is null ? null : ParseTime(r.Time),
                r.Method is null ? null : ParseMode(r.Method), r.TodayMethod is not null,
                r.TodayMethod is null ? null : ParseMode(r.TodayMethod)));
    }

    private static string Format(DateTimeOffset time) => time.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);
    private static DateTimeOffset ParseTime(string value) => DateTimeOffset.Parse(value, CultureInfo.InvariantCulture);
    private static WebGameLoginMode ParseMode(string value) => Enum.TryParse<WebGameLoginMode>(value, out var mode) &&
        Enum.IsDefined(mode) && mode.ToString() == value ? mode : throw new InvalidOperationException("Invalid login detection mode.");
    private sealed class SettingsRow
    {
        public string Mode { get; set; } = "";
        public string TargetOrigin { get; set; } = "";
        public string SuccessUrl { get; set; } = "";
        public string SuccessSelector { get; set; } = "";
        public string JavaScript { get; set; } = "";
        public string NetworkRuleJson { get; set; } = "";
    }
    private sealed class HistoryRow
    {
        public long Id { get; set; }
        public long GameId { get; set; }
        public string Time { get; set; } = "";
        public string Method { get; set; } = "";
    }
    private sealed class SummaryRow
    {
        public long GameId { get; set; }
        public string? Time { get; set; }
        public string? Method { get; set; }
        public string? TodayMethod { get; set; }
    }
}
