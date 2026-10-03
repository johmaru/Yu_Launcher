using System.Text.Json;
using Xunit;
using YuLauncher.Core.lib;

namespace YHuLauncherBackEndTestUnit;

public sealed class WebGameEventTests
{
    [Theory]
    [InlineData("file:///tmp", "GET", 200)]
    [InlineData("https://user:pass@example.test/a", "GET", 200)]
    [InlineData("https://example.test/a?q=secret", "GET", 200)]
    [InlineData("https://example.test/a#secret", "GET", 200)]
    [InlineData("https://example.test/a", "", 200)]
    [InlineData("https://example.test/a", "GET X", 200)]
    [InlineData("https://example.test/a", "GET\n", 200)]
    [InlineData("https://example.test/a", "GET", 99)]
    [InlineData("https://example.test/a", "GET", 600)]
    public void InvalidMetadataIsRejected(string url, string method, int status) =>
        Assert.Throws<ArgumentException>(() => WebGameNetworkConditions.Normalize(new(url, method, status)));

    [Fact]
    public void MetadataNormalizesWithoutChangingPathCase()
    {
        var rule = WebGameNetworkConditions.Normalize(new("https://EXAMPLE.test:443/Login", "post", 200));
        Assert.Equal("https://example.test/Login", rule.Url);
        Assert.Equal("POST", rule.Method);
        Assert.True(WebGameNetworkConditions.MatchesMetadata(rule, "https://example.test/Login?private=x#y", "POST", 200));
        Assert.False(WebGameNetworkConditions.MatchesMetadata(rule, "https://example.test/login", "POST", 200));
        Assert.False(WebGameNetworkConditions.MatchesMetadata(rule, rule.Url, "GET", 200));
        Assert.False(WebGameNetworkConditions.MatchesMetadata(rule, rule.Url, "POST", 401));
    }

    [Theory]
    [InlineData("bad")]
    [InlineData("/~2")]
    [InlineData("/~")]
    public void InvalidPointerIsRejected(string pointer) => Assert.Throws<ArgumentException>(() => WebGameEventJson.ParsePointer(pointer));

    [Theory]
    [InlineData("/a~1b/~0", "null", true)]
    [InlineData("/missing", "null", false)]
    [InlineData("/values/0", "1.0", true)]
    [InlineData("/values/0", "\"1\"", false)]
    [InlineData("/values/01", "1", false)]
    [InlineData("/values/-", "1", false)]
    [InlineData("/values/-1", "1", false)]
    [InlineData("/values/2", "1", false)]
    [InlineData("/flag", "true", true)]
    [InlineData("/flag", "\"true\"", false)]
    [InlineData("/Flag", "true", false)]
    public void JsonConditionsRespectTypesAndPointerBoundaries(string pointer, string expected, bool matches)
    {
        using var json = JsonDocument.Parse("{\"a/b\":{\"~\":null},\"values\":[1],\"flag\":true}");
        using var condition = new WebGameNetworkConditions(new("https://example.test/a", "GET", 200, pointer, expected));
        Assert.Equal(matches, condition.MatchesJson(json.RootElement));
    }

    [Fact]
    public void RootScalarAndPairValidation()
    {
        using var json = JsonDocument.Parse("1.0");
        using var rule = new WebGameNetworkConditions(new("https://example.test/a", "GET", 200, "", "1"));
        Assert.True(rule.MatchesJson(json.RootElement));
        foreach (var invalid in new[] { new WebGameNetworkRule("https://example.test/a", "GET", 200, "", null),
            new("https://example.test/a", "GET", 200, null, "true"), new("https://example.test/a", "GET", 200, "", "{}"),
            new("https://example.test/a", "GET", 200, "", "[]"), new("https://example.test/a", "GET", 200, "", "invalid") })
            Assert.Throws<ArgumentException>(() => WebGameNetworkConditions.Normalize(invalid));
    }
    [Fact]
    public void SizeLimitsAndSecretBranchesAreEnforced()
    {
        var rule = new WebGameNetworkRule("https://example.test/login", "GET", 200);
        foreach (var invalid in new[] {
            rule with { Url = "https://example.test/" + new string('x', 8192) },
            rule with { Method = new string('A', 65) },
            rule with { JsonPointer = "/" + new string('x', 4096), ExpectedJson = "true" },
            rule with { JsonPointer = "", ExpectedJson = JsonSerializer.Serialize(new string('あ', 700)) }
        }) Assert.Throws<ArgumentException>(() => WebGameNetworkConditions.Normalize(invalid));
        using var input = JsonDocument.Parse("{\"safe\":{\"success\":true},\"USER_PROFILE\":{\"email\":\"private\"},\"nested\":{\"sessionId\":\"private\",\"count\":1}}");
        using var redacted = JsonDocument.Parse(WebGameEventJson.Redact(input.RootElement));
        Assert.Equal("[redacted]", redacted.RootElement.GetProperty("USER_PROFILE").GetString());
        Assert.Equal("[redacted]", redacted.RootElement.GetProperty("nested").GetProperty("sessionId").GetString());
        Assert.True(redacted.RootElement.GetProperty("safe").GetProperty("success").GetBoolean());
        Assert.Equal(1, redacted.RootElement.GetProperty("nested").GetProperty("count").GetInt32());
    }
}

