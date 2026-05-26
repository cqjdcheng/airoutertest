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
        }, "anthropic", "11111111-1111-1111-1111-111111111111");

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
        json.Should().Contain("Claude Agent SDK");
        json.Should().Contain("Reply with exactly OK");
        json.Should().Contain("\"cache_control\":{\"type\":\"ephemeral\"}");
        json.Should().Contain("\"content\":[{\"type\":\"text\"");
        json.Should().Contain("\"thinking\":{\"type\":\"adaptive\"}");
        json.Should().Contain("\"context_management\"");
        json.Should().Contain("clear_thinking_20251015");
        json.Should().Contain("\"output_config\":{\"effort\":\"xhigh\"}");
        json.Should().Contain("\"metadata\"");
        json.Should().Contain("\"tools\"");
        json.Should().Contain("\"Agent\"");
        json.Should().Contain("\"TaskCreate\"");
        json.Should().NotContain("knowledge_answer");
        ((object[])payload["tools"]!).Should().HaveCount(26);
        payload["stream"].Should().Be(true);
        payload["model"].Should().Be("claude-opus-4-7");
    }

    [Fact]
    public void BuildProbePayload_ShouldForceStream_ForAnthropic()
    {
        var payload = BuildProbePayload(new CreateSelfTestRequest
        {
            SiteUrl = "https://relay.example.com",
            ModelName = "claude-opus-4-7",
            ApiKey = "sk-test",
            ApiType = "anthropic",
            IsStream = false
        }, "anthropic", "22222222-2222-2222-2222-222222222222");

        payload["stream"].Should().Be(true);
    }

    [Fact]
    public void ApplyAuthHeaders_ShouldMimicClaudeCodeCli_ForAnthropic()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://relay.example.com/v1/messages");

        ApplyAuthHeaders(request, "anthropic", "sk-test", "33333333-3333-3333-3333-333333333333");

        request.Headers.Contains("x-api-key").Should().BeFalse();
        request.Headers.Authorization.Should().NotBeNull();
        request.Headers.Authorization!.Scheme.Should().Be("Bearer");
        request.Headers.Authorization.Parameter.Should().Be("sk-test");
        request.Headers.GetValues("anthropic-version").Should().Contain("2023-06-01");
        request.Headers.GetValues("anthropic-beta").Single().Should().Be("claude-code-20250219,interleaved-thinking-2025-05-14,context-management-2025-06-27,prompt-caching-scope-2026-01-05,effort-2025-11-24");
        request.Headers.GetValues("x-app").Should().Contain("cli");
        request.Headers.GetValues("x-claude-code-session-id").Should().Contain("33333333-3333-3333-3333-333333333333");
        request.Headers.GetValues("X-Stainless-Package-Version").Should().Contain("0.94.0");
        request.Headers.GetValues("X-Stainless-Runtime-Version").Should().Contain("v24.3.0");
        request.Headers.GetValues("anthropic-dangerous-direct-browser-access").Should().Contain("true");
        request.Headers.UserAgent.ToString().Should().Be("claude-cli/2.1.150 (external, sdk-cli)");
    }

    [Fact]
    public void BuildProbePayload_ShouldUseMinimalAckProbe_ForOpenAi()
    {
        var payload = BuildProbePayload(new CreateSelfTestRequest
        {
            SiteUrl = "https://relay.example.com",
            ModelName = "gpt-5.5",
            ApiKey = "sk-test",
            ApiType = "openai",
            IsStream = true
        }, "openai");

        payload.Should().NotContainKey("temperature");
        payload.Should().NotContainKey("stream_options");
        JsonSerializer.Serialize(payload).Should().Contain("Reply with exactly OK");
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
        payload.Should().NotContainKey("stream_options");
        payload["model"].Should().Be("claude-opus-4.7");
    }

    [Fact]
    public void TryBuildEndpoint_ShouldUseAnthropicBetaMessagesEndpoint()
    {
        var succeeded = TryBuildEndpoint("https://relay.example.com", "anthropic", out var endpoint, out var error);

        succeeded.Should().BeTrue(error);
        endpoint.ToString().Should().Be("https://relay.example.com/v1/messages?beta=true");
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
    public void EvaluateProbeResponse_ShouldUseClaudeCodeChecks_ForAnthropic()
    {
        using var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK);
        response.Content = new StringContent("");
        var probeResponse = CreateProbeResponse("OK", "claude-opus-4-7", "anthropic_stream", isStream: true);

        var result = EvaluateProbeResponse(
            new CreateSelfTestRequest { ModelName = "claude-code-4.7", IsStream = true },
            "anthropic",
            new Uri("https://relay.example.com/v1/messages?beta=true"),
            response,
            probeResponse);

        result.Checks.Select(check => check.Code).Should().Contain("CC3");
        result.Checks.Select(check => check.Code).Should().NotContain(["D4", "D7", "S1"]);
    }

    [Fact]
    public void EvaluateProbeResponse_ShouldUseCodexChecks_ForOpenAi()
    {
        using var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK);
        response.Content = new StringContent("");
        var probeResponse = CreateProbeResponse("OK", "codex-mini-latest", "openai_chat_stream", isStream: true);

        var result = EvaluateProbeResponse(
            new CreateSelfTestRequest { ModelName = "codex-mini-latest", IsStream = true },
            "openai",
            new Uri("https://relay.example.com/v1/chat/completions"),
            response,
            probeResponse);

        result.Checks.Select(check => check.Code).Should().Contain("CO3");
        result.Checks.Select(check => check.Code).Should().NotContain(["D4", "D7", "S1"]);
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

    private static Dictionary<string, object?> BuildProbePayload(CreateSelfTestRequest request, string apiType, string? claudeSessionId = null)
    {
        return InvokePrivate<Dictionary<string, object?>>("BuildProbePayload", request, apiType, claudeSessionId);
    }

    private static SelfTestProbeResult BuildFullModelProbe(string requestedModel, string? returnedModel, string content)
    {
        return InvokePrivate<SelfTestProbeResult>("BuildFullModelProbe", requestedModel, returnedModel, content);
    }

    private static SelfTestExecutionResult EvaluateProbeResponse(CreateSelfTestRequest request, string apiType, Uri endpoint, HttpResponseMessage response, object probeResponse)
    {
        return InvokePrivate<SelfTestExecutionResult>("EvaluateProbeResponse", request, apiType, endpoint, response, probeResponse);
    }

    private static object CreateProbeResponse(string content, string returnedModel, string responseShape, bool isStream)
    {
        var probeType = typeof(OpenAiCompatibleSelfTestRunner).GetNestedType("ProbeResponse", BindingFlags.NonPublic);
        probeType.Should().NotBeNull();
        var constructor = probeType!
            .GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
            .Single(item => item.GetParameters().Length == 10);
        return constructor.Invoke(
        [
            100,
            300,
            content,
            null,
            returnedModel,
            true,
            responseShape,
            isStream,
            2,
            isStream
        ]);
    }

    private static (decimal MatchScore, decimal RiskScore, string RiskLevel) CalculateSelfTestScore(IReadOnlyList<SelfTestProbeResult> checks)
    {
        return InvokePrivate<(decimal MatchScore, decimal RiskScore, string RiskLevel)>("CalculateSelfTestScore", checks);
    }

    private static string BuildSummary(string status, decimal matchScore, IReadOnlyList<SelfTestProbeResult> checks)
    {
        return InvokePrivate<string>("BuildSummary", status, matchScore, checks);
    }

    private static void ApplyAuthHeaders(HttpRequestMessage request, string apiType, string apiKey, string? claudeSessionId = null)
    {
        InvokePrivate<object?>("ApplyAuthHeaders", request, apiType, apiKey, claudeSessionId);
    }

    private static bool TryBuildEndpoint(string rawUrl, string apiType, out Uri endpoint, out string error)
    {
        var parameters = new object?[] { rawUrl, apiType, null, string.Empty };
        var result = InvokePrivateWithParameters<bool>("TryBuildEndpoint", parameters);
        endpoint = (Uri)parameters[2]!;
        error = (string)parameters[3]!;
        return result;
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
        return InvokePrivateWithParameters<T>(methodName, parameters);
    }

    private static T InvokePrivateWithParameters<T>(string methodName, object?[] parameters)
    {
        var method = typeof(OpenAiCompatibleSelfTestRunner).GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static);
        method.Should().NotBeNull();
        return ((T?)method!.Invoke(null, parameters))!;
    }
}
