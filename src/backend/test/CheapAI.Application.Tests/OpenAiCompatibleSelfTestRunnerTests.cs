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
