using System.Reflection;
using System.Text.Json;
using CheapAI.Application.Participation;
using CheapAI.Infrastructure.Participation;
using FluentAssertions;

namespace CheapAI.Application.Tests;

public sealed class OpenAiCompatibleSelfTestRunnerTests
{
    [Fact]
    public void BuildProbePayload_ShouldOmitTemperature_ForAnthropic()
    {
        var payload = BuildProbePayload(new CreateSelfTestRequest
        {
            SiteUrl = "https://relay.example.com",
            ModelName = "claude-opus-4.7",
            ApiKey = "sk-test",
            ApiType = "anthropic",
            IsStream = true
        }, "anthropic");

        payload.Should().NotContainKey("temperature");
        payload.Should().ContainKey("max_tokens");
        payload.Should().ContainKey("stream");
        payload["model"].Should().Be("claude-opus-4-7");
    }

    [Fact]
    public void BuildProbePayload_ShouldUseClaudeCodeStyleBlocks_ForAnthropic()
    {
        var payload = BuildProbePayload(new CreateSelfTestRequest
        {
            SiteUrl = "https://relay.example.com",
            ModelName = "claude-code-4.7",
            ApiKey = "sk-test",
            ApiType = "anthropic",
            IsStream = true
        }, "anthropic");

        var json = JsonSerializer.Serialize(payload);

        json.Should().Contain("\"system\"");
        json.Should().Contain("Claude Code");
        json.Should().Contain("\"content\":[{\"type\":\"text\"");
        json.Should().Contain("\"metadata\"");
    }

    [Fact]
    public void ApplyAuthHeaders_ShouldMimicClaudeCodeCli_ForAnthropic()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://relay.example.com/v1/messages");

        ApplyAuthHeaders(request, "anthropic", "sk-test");

        request.Headers.GetValues("x-api-key").Should().Contain("sk-test");
        request.Headers.Authorization.Should().NotBeNull();
        request.Headers.Authorization!.Scheme.Should().Be("Bearer");
        request.Headers.Authorization.Parameter.Should().Be("sk-test");
        request.Headers.GetValues("anthropic-version").Should().Contain("2023-06-01");
        request.Headers.GetValues("anthropic-beta").Should().Contain("claude-code-20250219");
        request.Headers.GetValues("x-claude-code-session-id").Should().Contain("cheapai-self-test");
        request.Headers.UserAgent.ToString().Should().Contain("claude-code");
    }

    [Fact]
    public void BuildProbePayload_ShouldKeepDeterministicTemperature_ForOpenAi()
    {
        var payload = BuildProbePayload(new CreateSelfTestRequest
        {
            SiteUrl = "https://relay.example.com",
            ModelName = "gpt-5.5",
            ApiKey = "sk-test",
            ApiType = "openai",
            IsStream = true
        }, "openai");

        payload.Should().ContainKey("temperature");
        payload["temperature"].Should().Be(0);
        payload.Should().ContainKey("stream_options");
    }

    [Fact]
    public void BuildProbePayload_ShouldOmitTemperature_ForClaudeOnOpenAiCompatibleEndpoint()
    {
        var payload = BuildProbePayload(new CreateSelfTestRequest
        {
            SiteUrl = "https://relay.example.com",
            ModelName = "claude-opus-4.7",
            ApiKey = "sk-test",
            ApiType = "openai",
            IsStream = true
        }, "openai");

        payload.Should().NotContainKey("temperature");
        payload.Should().ContainKey("stream_options");
        payload["model"].Should().Be("claude-opus-4.7");
    }

    [Fact]
    public void BuildFullModelProbe_ShouldPass_WhenReturnedModelMatchesRequest()
    {
        var probe = BuildFullModelProbe(
            "claude-opus-4.7",
            "claude-opus-4-7",
            """{"model_claim":"Claude Opus 4.7"}""");

        probe.Code.Should().Be("D9");
        probe.Status.Should().Be("pass");
        probe.Confidence.Should().Be("high");
    }

    [Fact]
    public void BuildFullModelProbe_ShouldWarn_WhenReturnedModelDiffersFromRequest()
    {
        var probe = BuildFullModelProbe(
            "claude-opus-4.7",
            "claude-sonnet-4-5",
            """{"model_claim":"Claude Sonnet"}""");

        probe.Code.Should().Be("D9");
        probe.Status.Should().Be("warn");
        probe.RiskImpact.Should().BeGreaterThan(0);
    }

    [Fact]
    public void CalculateSelfTestScore_ShouldDeductWarningsAndUnknownEvidence()
    {
        var checks = new[]
        {
            new SelfTestProbeResult { Code = "D1", Name = "协议连通性", Category = "协议", Status = "pass", Confidence = "high", ScoreImpact = 15, RiskImpact = 0 },
            new SelfTestProbeResult { Code = "D8", Name = "响应时延", Category = "性能", Status = "warn", Confidence = "medium", ScoreImpact = 5, RiskImpact = 8 },
            new SelfTestProbeResult { Code = "S1", Name = "Token 用量", Category = "安全", Status = "warn", Confidence = "medium", ScoreImpact = 0, RiskImpact = 18 },
            new SelfTestProbeResult { Code = "D3", Name = "身份一致性", Category = "身份", Status = "unknown", Confidence = "low", ScoreImpact = 0, RiskImpact = 6 }
        };

        var score = CalculateSelfTestScore(checks);

        score.MatchScore.Should().Be(68);
        score.RiskScore.Should().Be(32);
        score.RiskLevel.Should().Be("medium");
    }

    [Fact]
    public void BuildIdentityProbe_ShouldDeduct_WhenIdentityCannotBeRead()
    {
        var probe = BuildIdentityProbe("claude-opus-4.7", """{"knowledge_answer":"9.8"}""");

        probe.Code.Should().Be("D3");
        probe.Status.Should().Be("unknown");
        probe.RiskImpact.Should().BeGreaterThan(0);
    }

    [Fact]
    public void BuildSummary_ShouldUseProbeNamesAndDeductionText()
    {
        var checks = new[]
        {
            new SelfTestProbeResult { Code = "D8", Name = "响应时延", Category = "性能", Status = "warn", Confidence = "medium", ScoreImpact = 5, RiskImpact = 8 },
            new SelfTestProbeResult { Code = "S1", Name = "Token 用量", Category = "安全", Status = "warn", Confidence = "medium", ScoreImpact = 0, RiskImpact = 18 },
            new SelfTestProbeResult { Code = "D3", Name = "身份一致性", Category = "身份", Status = "unknown", Confidence = "low", ScoreImpact = 0, RiskImpact = 6 }
        };

        var summary = BuildSummary("succeeded", 68, checks);

        summary.Should().Contain("响应时延-扣8");
        summary.Should().Contain("Token 用量-扣18");
        summary.Should().Contain("身份一致性-扣6");
        summary.Should().NotContain("D8");
        summary.Should().NotContain("S1");
        summary.Should().NotContain("D3");
    }

    [Fact]
    public void ExtractReturnedModel_ShouldReadAnthropicMessageStartModel()
    {
        using var document = JsonDocument.Parse("""{"type":"message_start","message":{"model":"claude-opus-4-7"}}""");

        var model = ExtractReturnedModel(document.RootElement, "anthropic");

        model.Should().Be("claude-opus-4-7");
    }

    [Fact]
    public void LooksLikeSuccessResponse_ShouldAcceptOpenAiResponsesApiShape()
    {
        using var document = JsonDocument.Parse("""
            {
              "id": "resp_123",
              "model": "gpt-5.5",
              "output_text": "{\"model_claim\":\"GPT\",\"knowledge_answer\":\"9.8\"}",
              "usage": { "input_tokens": 10, "output_tokens": 20 }
            }
            """);

        LooksLikeSuccessResponse(document.RootElement, "openai").Should().BeTrue();
        ExtractMessageContent(document.RootElement, "openai").Should().Contain("knowledge_answer");
        ExtractReturnedModel(document.RootElement, "openai").Should().Be("gpt-5.5");
    }

    [Fact]
    public void ExtractMessageContent_ShouldReadNestedOpenAiResponsesOutput()
    {
        using var document = JsonDocument.Parse("""
            {
              "output": [
                {
                  "type": "message",
                  "content": [
                    { "type": "output_text", "text": "{\"short_answer\":\"OK\"}" }
                  ]
                }
              ]
            }
            """);

        LooksLikeSuccessResponse(document.RootElement, "openai").Should().BeTrue();
        ExtractMessageContent(document.RootElement, "openai").Should().Be("""{"short_answer":"OK"}""");
    }

    [Fact]
    public void LooksLikeStreamDelta_ShouldAcceptOpenAiResponsesStreamEvent()
    {
        using var document = JsonDocument.Parse("""{"type":"response.output_text.delta","delta":"{\"short_answer\":\"OK\"}"}""");

        LooksLikeStreamDelta(document.RootElement, "openai").Should().BeTrue();
        ExtractDeltaContent(document.RootElement, "openai").Should().Be("""{"short_answer":"OK"}""");
    }

    private static Dictionary<string, object?> BuildProbePayload(CreateSelfTestRequest request, string apiType)
    {
        return InvokePrivate<Dictionary<string, object?>>("BuildProbePayload", request, apiType);
    }

    private static SelfTestProbeResult BuildFullModelProbe(string requestedModel, string? returnedModel, string content)
    {
        return InvokePrivate<SelfTestProbeResult>("BuildFullModelProbe", requestedModel, returnedModel, content);
    }

    private static (decimal MatchScore, decimal RiskScore, string RiskLevel) CalculateSelfTestScore(IReadOnlyList<SelfTestProbeResult> checks)
    {
        return InvokePrivate<(decimal MatchScore, decimal RiskScore, string RiskLevel)>("CalculateSelfTestScore", checks);
    }

    private static SelfTestProbeResult BuildIdentityProbe(string requestedModel, string content)
    {
        return InvokePrivate<SelfTestProbeResult>("BuildIdentityProbe", requestedModel, content);
    }

    private static string BuildSummary(string status, decimal matchScore, IReadOnlyList<SelfTestProbeResult> checks)
    {
        return InvokePrivate<string>("BuildSummary", status, matchScore, checks);
    }

    private static void ApplyAuthHeaders(HttpRequestMessage request, string apiType, string apiKey)
    {
        InvokePrivate<object?>("ApplyAuthHeaders", request, apiType, apiKey);
    }

    private static string? ExtractReturnedModel(JsonElement root, string apiType)
    {
        return InvokePrivate<string?>("ExtractReturnedModel", root, apiType);
    }

    private static bool LooksLikeSuccessResponse(JsonElement root, string apiType)
    {
        return InvokePrivate<bool>("LooksLikeSuccessResponse", root, apiType);
    }

    private static bool LooksLikeStreamDelta(JsonElement root, string apiType)
    {
        return InvokePrivate<bool>("LooksLikeStreamDelta", root, apiType);
    }

    private static string ExtractMessageContent(JsonElement root, string apiType)
    {
        return InvokePrivate<string>("ExtractMessageContent", root, apiType);
    }

    private static string ExtractDeltaContent(JsonElement root, string apiType)
    {
        return InvokePrivate<string>("ExtractDeltaContent", root, apiType);
    }

    private static T InvokePrivate<T>(string methodName, params object?[] parameters)
    {
        var method = typeof(OpenAiCompatibleSelfTestRunner).GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static);
        method.Should().NotBeNull();
        return ((T?)method!.Invoke(null, parameters))!;
    }
}
