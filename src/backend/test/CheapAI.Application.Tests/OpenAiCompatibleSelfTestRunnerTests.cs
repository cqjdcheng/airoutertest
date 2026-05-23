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

    private static T InvokePrivate<T>(string methodName, params object?[] parameters)
    {
        var method = typeof(OpenAiCompatibleSelfTestRunner).GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static);
        method.Should().NotBeNull();
        return ((T?)method!.Invoke(null, parameters))!;
    }
}
