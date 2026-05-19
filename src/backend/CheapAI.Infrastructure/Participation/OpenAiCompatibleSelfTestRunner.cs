using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using CheapAI.Application.Participation;

namespace CheapAI.Infrastructure.Participation;

public sealed class OpenAiCompatibleSelfTestRunner : ISelfTestRunner
{
    private const int EstimatedTokenLimit = 1000;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(18)
    };

    public async Task<SelfTestExecutionResult> ExecuteAsync(CreateSelfTestRequest request, CancellationToken cancellationToken = default)
    {
        if (!TryBuildChatCompletionsEndpoint(request.SiteUrl, out var endpoint, out var error))
        {
            return Failed(75, "high", error, [
                Probe("D1", "协议连通性", "协议", "fail", "high", 0, 40, error)
            ]);
        }

        if (IsReservedHost(endpoint.Host))
        {
            return Failed(30, "medium", "The test URL is a reserved domain. External request was skipped for local automation; real relay sites will be tested with an actual request.", [
                Probe("D1", "协议连通性", "协议", "warn", "high", 8, 12, "Reserved example domain was not requested.")
            ]);
        }

        if (IsBlockedNetworkHost(endpoint.Host))
        {
            return Failed(90, "high", "The test URL points to a local, private, or metadata network host and was blocked.", [
                Probe("D1", "协议连通性", "协议", "fail", "high", 0, 55, "Local, private, link-local, or metadata network host is blocked.")
            ]);
        }

        if (string.IsNullOrWhiteSpace(request.ApiKey))
        {
            return Failed(70, "high", "API Key is required to run a real model request.", [
                Probe("D1", "协议连通性", "协议", "fail", "high", 0, 40, "Missing API Key.")
            ]);
        }

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(20));

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint);
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", request.ApiKey);
        httpRequest.Content = new StringContent(JsonSerializer.Serialize(BuildProbePayload(request), JsonOptions), Encoding.UTF8, "application/json");

        var stopwatch = Stopwatch.StartNew();
        try
        {
            using var response = await HttpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, timeoutCts.Token);
            var responseHeaderMs = (int)Math.Min(stopwatch.ElapsedMilliseconds, int.MaxValue);

            if (!response.IsSuccessStatusCode)
            {
                stopwatch.Stop();
                return FromHttpFailure(response.StatusCode, responseHeaderMs, (int)Math.Min(stopwatch.ElapsedMilliseconds, int.MaxValue), response);
            }

            var probeResponse = request.IsStream
                ? await ReadStreamingResponseAsync(response, stopwatch, responseHeaderMs, timeoutCts.Token)
                : await ReadJsonResponseAsync(response, stopwatch, responseHeaderMs, timeoutCts.Token);

            return EvaluateProbeResponse(request, endpoint, response, probeResponse);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            stopwatch.Stop();
            return Failed(55, "medium", "The request timed out. The relay may be unreachable, slow, or unstable.", [
                Probe("D1", "协议连通性", "协议", "fail", "high", 0, 30, "Request timeout after 20 seconds."),
                Probe("D8", "响应时延", "性能", "fail", "high", 0, 25, "Full response exceeded timeout.")
            ], fullResponseMs: (int)Math.Min(stopwatch.ElapsedMilliseconds, int.MaxValue));
        }
        catch (HttpRequestException exception)
        {
            stopwatch.Stop();
            return Failed(50, "medium", $"Request failed: {exception.Message}", [
                Probe("D1", "协议连通性", "协议", "fail", "high", 0, 30, "HTTP request exception."),
                Probe("S4", "错误响应泄露", "安全", "unknown", "low", 0, 0, "No relay error body was exposed to CheapAI.")
            ], fullResponseMs: (int)Math.Min(stopwatch.ElapsedMilliseconds, int.MaxValue));
        }
        catch (JsonException exception)
        {
            stopwatch.Stop();
            return Failed(65, "high", $"Response parsing failed: {exception.Message}", [
                Probe("D2", "响应结构", "协议", "fail", "high", 0, 35, "Response is not valid JSON / SSE JSON."),
                Probe("D5", "内容完整性", "完整性", "fail", "high", 0, 20, "Response cannot be parsed.")
            ], fullResponseMs: (int)Math.Min(stopwatch.ElapsedMilliseconds, int.MaxValue));
        }
    }

    private static Dictionary<string, object?> BuildProbePayload(CreateSelfTestRequest request)
    {
        var payload = new Dictionary<string, object?>
        {
            ["model"] = request.ModelName,
            ["messages"] = new object[]
            {
                new
                {
                    role = "user",
                    content = """
                    CheapAI relay verification probe. Return strict JSON only, without Markdown fences:
                    {
                      "model_claim": "the model identity you claim",
                      "knowledge_answer": "answer only 9.11 or 9.8: which number is larger?",
                      "protocol_ack": "openai-chat-completions-compatible",
                      "format_ack": "json-ok",
                      "short_answer": "OK"
                    }
                    """
                }
            },
            ["temperature"] = 0,
            ["max_tokens"] = 260,
            ["stream"] = request.IsStream
        };

        if (request.IsStream)
        {
            payload["stream_options"] = new { include_usage = true };
        }

        return payload;
    }

    private static async Task<ProbeResponse> ReadJsonResponseAsync(
        HttpResponseMessage response,
        Stopwatch stopwatch,
        int responseHeaderMs,
        CancellationToken cancellationToken)
    {
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        stopwatch.Stop();

        using var document = JsonDocument.Parse(body);
        var root = document.RootElement.Clone();
        var content = ExtractMessageContent(root);
        var usage = ExtractUsage(root);

        return new ProbeResponse(
            responseHeaderMs,
            (int)Math.Min(stopwatch.ElapsedMilliseconds, int.MaxValue),
            content,
            usage,
            OpenAiShapeValid: LooksLikeOpenAiChatCompletion(root),
            StreamIntegrityValid: null,
            StreamChunkCount: null,
            StreamDoneSeen: null);
    }

    private static async Task<ProbeResponse> ReadStreamingResponseAsync(
        HttpResponseMessage response,
        Stopwatch stopwatch,
        int responseHeaderMs,
        CancellationToken cancellationToken)
    {
        int? firstTokenMs = null;
        var chunkCount = 0;
        var doneSeen = false;
        var shapeValid = true;
        var contentBuilder = new StringBuilder();
        TokenUsage? usage = null;

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream, Encoding.UTF8);

        while (true)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (line is null)
            {
                break;
            }

            if (string.IsNullOrWhiteSpace(line) ||
                !line.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var data = line["data:".Length..].Trim();
            if (data.Equals("[DONE]", StringComparison.OrdinalIgnoreCase))
            {
                doneSeen = true;
                break;
            }

            using var document = JsonDocument.Parse(data);
            var root = document.RootElement.Clone();
            if (!LooksLikeOpenAiStreamDelta(root))
            {
                shapeValid = false;
                continue;
            }

            var delta = ExtractDeltaContent(root);
            if (!string.IsNullOrEmpty(delta))
            {
                firstTokenMs ??= (int)Math.Min(stopwatch.ElapsedMilliseconds, int.MaxValue);
                contentBuilder.Append(delta);
            }

            usage ??= ExtractUsage(root);
            chunkCount++;
        }

        stopwatch.Stop();
        var streamIntegrityValid = chunkCount > 0 && (doneSeen || response.Content.Headers.ContentType?.MediaType?.Contains("event-stream", StringComparison.OrdinalIgnoreCase) == true);

        return new ProbeResponse(
            firstTokenMs ?? responseHeaderMs,
            (int)Math.Min(stopwatch.ElapsedMilliseconds, int.MaxValue),
            contentBuilder.ToString(),
            usage,
            OpenAiShapeValid: shapeValid && chunkCount > 0,
            StreamIntegrityValid: streamIntegrityValid,
            StreamChunkCount: chunkCount,
            StreamDoneSeen: doneSeen);
    }

    private static SelfTestExecutionResult EvaluateProbeResponse(
        CreateSelfTestRequest request,
        Uri endpoint,
        HttpResponseMessage httpResponse,
        ProbeResponse response)
    {
        var checks = new List<SelfTestProbeResult>
        {
            Probe("D1", "协议连通性", "协议", "pass", "high", 15, 0, $"HTTP {(int)httpResponse.StatusCode}; endpoint {endpoint.Host}."),
            Probe("D2", "响应结构", "协议", response.OpenAiShapeValid ? "pass" : "fail", "high", response.OpenAiShapeValid ? 20 : 0, response.OpenAiShapeValid ? 0 : 35, response.OpenAiShapeValid ? "OpenAI compatible choices structure detected." : "Missing choices / message / delta structure.")
        };

        checks.Add(BuildContentIntegrityProbe(response.Content));
        checks.Add(BuildStructuredOutputProbe(response.Content));
        checks.Add(BuildKnowledgeProbe(response.Content));
        checks.Add(BuildLatencyProbe(response.FirstTokenMs, response.FullResponseMs));
        checks.Add(BuildTokenProbe(response.Usage));
        checks.Add(BuildIdentityProbe(request.ModelName, response.Content));
        checks.Add(BuildUpstreamFingerprintProbe(httpResponse));

        if (request.IsStream)
        {
            checks.Add(Probe(
                "S5",
                "流完整性",
                "安全",
                response.StreamIntegrityValid == true ? "pass" : "warn",
                "high",
                response.StreamIntegrityValid == true ? 10 : 4,
                response.StreamIntegrityValid == true ? 0 : 12,
                response.StreamIntegrityValid == true
                    ? $"SSE chunks={response.StreamChunkCount}; done={response.StreamDoneSeen}."
                    : $"Stream completed with weak integrity signal; chunks={response.StreamChunkCount}; done={response.StreamDoneSeen}."));
        }
        else
        {
            checks.Add(Probe("S5", "流完整性", "安全", "unknown", "high", 0, 0, "Non-stream request; stream integrity was not tested."));
        }

        var matchScore = Math.Clamp(checks.Sum(x => x.ScoreImpact), 0, 100);
        var riskScore = Math.Clamp(100 - matchScore + checks.Sum(x => x.RiskImpact), 0, 100);
        var status = checks.Any(x => x.Status == "fail" && x.Confidence == "high") ? "failed" : "succeeded";
        var riskLevel = riskScore >= 70 ? "high" : riskScore >= 30 ? "medium" : "low";
        var tokensPerSecond = CalculateTokensPerSecond(response.Usage?.OutputTokens, response.FullResponseMs);

        return new SelfTestExecutionResult
        {
            Status = status,
            FirstTokenMs = response.FirstTokenMs,
            FullResponseMs = response.FullResponseMs,
            RiskScore = riskScore,
            RiskLevel = riskLevel,
            ResultSummary = BuildSummary(status, matchScore, checks),
            MatchScore = matchScore,
            InputTokens = response.Usage?.InputTokens,
            OutputTokens = response.Usage?.OutputTokens,
            TotalTokens = response.Usage?.TotalTokens,
            EstimatedTokens = EstimatedTokenLimit,
            TokensPerSecond = tokensPerSecond,
            Checks = checks
        };
    }

    private static SelfTestProbeResult BuildContentIntegrityProbe(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return Probe("D5", "内容完整性", "完整性", "fail", "high", 0, 25, "Model content is empty.");
        }

        if (content.Length < 8)
        {
            return Probe("D5", "内容完整性", "完整性", "warn", "medium", 5, 8, "Model content is unusually short.");
        }

        return Probe("D5", "内容完整性", "完整性", "pass", "high", 10, 0, "Model returned non-empty content.");
    }

    private static SelfTestProbeResult BuildStructuredOutputProbe(string content)
    {
        return TryParseProbeJson(content, out _)
            ? Probe("D7", "结构化输出", "能力", "pass", "high", 15, 0, "Strict JSON probe output parsed successfully.")
            : Probe("D7", "结构化输出", "能力", "fail", "high", 0, 22, "Probe output is not strict JSON.");
    }

    private static SelfTestProbeResult BuildKnowledgeProbe(string content)
    {
        if (!TryParseProbeJson(content, out var root))
        {
            return Probe("D4", "知识能力", "能力", "unknown", "medium", 0, 0, "Knowledge answer could not be parsed because JSON output failed.");
        }

        var answer = TryGetString(root, "knowledge_answer").ToLowerInvariant();
        if (answer.Contains("9.8") || answer.Contains("9.80"))
        {
            return Probe("D4", "知识能力", "能力", "pass", "high", 20, 0, "Deterministic numeric comparison answered correctly.");
        }

        if (answer.Contains("9.11"))
        {
            return Probe("D4", "知识能力", "能力", "fail", "high", 0, 30, "Deterministic numeric comparison answered incorrectly.");
        }

        return Probe("D4", "知识能力", "能力", "warn", "medium", 8, 8, "Knowledge answer was ambiguous.");
    }

    private static SelfTestProbeResult BuildLatencyProbe(int? firstTokenMs, int fullResponseMs)
    {
        if (firstTokenMs is null)
        {
            return Probe("D8", "响应时延", "性能", "warn", "medium", 4, 8, $"First token was not measured; full response {fullResponseMs}ms.");
        }

        if (firstTokenMs <= 3000 && fullResponseMs <= 12000)
        {
            return Probe("D8", "响应时延", "性能", "pass", "high", 10, 0, $"First token {firstTokenMs}ms; full response {fullResponseMs}ms.");
        }

        if (firstTokenMs <= 15000 && fullResponseMs <= 20000)
        {
            return Probe("D8", "响应时延", "性能", "warn", "medium", 5, 8, $"Slow but completed; first token {firstTokenMs}ms; full response {fullResponseMs}ms.");
        }

        return Probe("D8", "响应时延", "性能", "fail", "high", 0, 20, $"Too slow; first token {firstTokenMs}ms; full response {fullResponseMs}ms.");
    }

    private static SelfTestProbeResult BuildTokenProbe(TokenUsage? usage)
    {
        if (usage is null || usage.TotalTokens is null)
        {
            return Probe("S1", "Token 注入", "安全", "unknown", "medium", 0, 0, "Usage tokens are missing; token injection cannot be verified.");
        }

        var totalTokens = usage.TotalTokens.Value;
        if (totalTokens > EstimatedTokenLimit * 1.2m)
        {
            return Probe("S1", "Token 注入", "安全", "warn", "medium", 0, 18, $"Usage total_tokens={totalTokens}, significantly above estimated {EstimatedTokenLimit}.");
        }

        return Probe("S1", "Token 注入", "安全", "pass", "medium", 0, 0, $"Usage total_tokens={totalTokens}; within expected range.");
    }

    private static SelfTestProbeResult BuildIdentityProbe(string modelName, string content)
    {
        if (!TryParseProbeJson(content, out var root))
        {
            return Probe("D3", "身份一致性", "身份", "unknown", "low", 0, 0, "Model claim is unavailable.");
        }

        var claim = TryGetString(root, "model_claim").ToLowerInvariant();
        var expectedFamily = GetExpectedModelFamily(modelName);
        if (string.IsNullOrWhiteSpace(claim) || expectedFamily == "unknown")
        {
            return Probe("D3", "身份一致性", "身份", "unknown", "low", 0, 0, "Model claim is insufficient for identity matching.");
        }

        var matched = claim.Contains(expectedFamily, StringComparison.OrdinalIgnoreCase) ||
            expectedFamily == "gpt" && (claim.Contains("openai", StringComparison.OrdinalIgnoreCase) || claim.Contains("gpt", StringComparison.OrdinalIgnoreCase)) ||
            expectedFamily == "claude" && (claim.Contains("anthropic", StringComparison.OrdinalIgnoreCase) || claim.Contains("claude", StringComparison.OrdinalIgnoreCase)) ||
            expectedFamily == "gemini" && (claim.Contains("google", StringComparison.OrdinalIgnoreCase) || claim.Contains("gemini", StringComparison.OrdinalIgnoreCase));

        return Probe(
            "D3",
            "身份一致性",
            "身份",
            matched ? "pass" : "warn",
            "low",
            0,
            matched ? 0 : 8,
            matched ? $"Model claim roughly matches {expectedFamily} family." : $"Model claim does not clearly match requested family {expectedFamily}.");
    }

    private static SelfTestProbeResult BuildUpstreamFingerprintProbe(HttpResponseMessage response)
    {
        var server = response.Headers.Server.ToString();
        var contentType = response.Content.Headers.ContentType?.MediaType ?? "unknown";
        var hasServerHeader = !string.IsNullOrWhiteSpace(server);
        var hasContentType = !contentType.Equals("unknown", StringComparison.OrdinalIgnoreCase);
        var evidence = hasServerHeader
            ? $"Captured weak upstream fingerprint: content-type={contentType}; server header present."
            : $"Captured weak upstream fingerprint: content-type={contentType}; no server header.";

        return hasServerHeader || hasContentType
            ? Probe("D6", "上游指纹", "信息", "pass", "low", 3, 0, evidence)
            : Probe("D6", "上游指纹", "信息", "unknown", "low", 0, 0, "No response header fingerprint was available.");
    }

    private static SelfTestExecutionResult FromHttpFailure(HttpStatusCode statusCode, int firstTokenMs, int fullResponseMs, HttpResponseMessage response)
    {
        var numericStatus = (int)statusCode;
        var isAuthFailure = statusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden;
        var checks = new List<SelfTestProbeResult>
        {
            Probe("D1", "协议连通性", "协议", "fail", "high", 0, isAuthFailure ? 18 : 35, $"Relay returned HTTP {numericStatus}."),
            Probe("D2", "响应结构", "协议", "unknown", "medium", 0, 0, "No successful model response to inspect."),
            Probe("S4", "错误响应泄露", "安全", "unknown", "low", 0, 0, "Error body is not persisted or exposed.")
        };

        var riskScore = isAuthFailure ? 35 : 65;
        return new SelfTestExecutionResult
        {
            Status = "failed",
            FirstTokenMs = firstTokenMs,
            FullResponseMs = fullResponseMs,
            RiskScore = riskScore,
            RiskLevel = isAuthFailure ? "medium" : "high",
            ResultSummary = isAuthFailure
                ? $"Relay returned {numericStatus}. Check API Key, model permission, or account balance."
                : $"Relay returned HTTP {numericStatus}. The request chain did not pass.",
            MatchScore = 0,
            EstimatedTokens = EstimatedTokenLimit,
            Checks = checks
        };
    }

    private static SelfTestExecutionResult Failed(
        decimal riskScore,
        string riskLevel,
        string message,
        IReadOnlyList<SelfTestProbeResult> checks,
        int? fullResponseMs = null)
    {
        return new SelfTestExecutionResult
        {
            Status = "failed",
            FullResponseMs = fullResponseMs,
            RiskScore = riskScore,
            RiskLevel = riskLevel,
            ResultSummary = message,
            MatchScore = Math.Clamp(checks.Sum(x => x.ScoreImpact), 0, 100),
            EstimatedTokens = EstimatedTokenLimit,
            Checks = checks
        };
    }

    private static string BuildSummary(string status, decimal matchScore, IReadOnlyList<SelfTestProbeResult> checks)
    {
        var failed = checks.Where(x => x.Status == "fail").Select(x => x.Code).ToArray();
        var warned = checks.Where(x => x.Status == "warn").Select(x => x.Code).ToArray();
        if (status == "succeeded" && failed.Length == 0)
        {
            return warned.Length == 0
                ? $"真实请求完成，P0 探针通过，匹配度 {matchScore:0.#}。API Key 未落库，自助测试不进入公共排行。"
                : $"真实请求完成，匹配度 {matchScore:0.#}，存在可疑项：{string.Join(", ", warned)}。";
        }

        return $"真实请求完成但关键探针未通过，匹配度 {matchScore:0.#}，失败项：{string.Join(", ", failed)}。";
    }

    private static decimal? CalculateTokensPerSecond(int? outputTokens, int fullResponseMs)
    {
        if (outputTokens is null or <= 0 || fullResponseMs <= 0)
        {
            return null;
        }

        return Math.Round(outputTokens.Value / (fullResponseMs / 1000m), 2);
    }

    private static SelfTestProbeResult Probe(
        string code,
        string name,
        string category,
        string status,
        string confidence,
        decimal scoreImpact,
        decimal riskImpact,
        string evidence)
    {
        return new SelfTestProbeResult
        {
            Code = code,
            Name = name,
            Category = category,
            Status = status,
            Confidence = confidence,
            ScoreImpact = scoreImpact,
            RiskImpact = riskImpact,
            Evidence = evidence
        };
    }

    private static bool TryBuildChatCompletionsEndpoint(string rawUrl, out Uri endpoint, out string error)
    {
        endpoint = null!;
        error = string.Empty;

        if (!Uri.TryCreate(rawUrl.Trim(), UriKind.Absolute, out var uri) ||
            uri.Scheme is not ("http" or "https"))
        {
            error = "Relay URL must be a full http or https URL.";
            return false;
        }

        if (uri.AbsolutePath.EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase))
        {
            endpoint = uri;
            return true;
        }

        var baseUrl = uri.ToString().TrimEnd('/');
        var suffix = uri.AbsolutePath.TrimEnd('/').EndsWith("/v1", StringComparison.OrdinalIgnoreCase)
            ? "/chat/completions"
            : "/v1/chat/completions";
        endpoint = new Uri(baseUrl + suffix);
        return true;
    }

    private static bool IsReservedHost(string host)
    {
        return host.EndsWith(".example", StringComparison.OrdinalIgnoreCase) ||
            host.Equals("example.com", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsBlockedNetworkHost(string host)
    {
        if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
            host.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase) ||
            host.EndsWith(".local", StringComparison.OrdinalIgnoreCase) ||
            host.EndsWith(".lan", StringComparison.OrdinalIgnoreCase) ||
            host.EndsWith(".internal", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (!IPAddress.TryParse(host, out var ipAddress))
        {
            return false;
        }

        if (IPAddress.IsLoopback(ipAddress) || ipAddress.IsIPv6LinkLocal || ipAddress.IsIPv6SiteLocal)
        {
            return true;
        }

        var rawBytes = ipAddress.GetAddressBytes();
        if (rawBytes.Length == 16)
        {
            return (rawBytes[0] & 0xfe) == 0xfc;
        }

        return rawBytes[0] == 10 ||
            rawBytes[0] == 127 ||
            rawBytes[0] == 169 && rawBytes[1] == 254 ||
            rawBytes[0] == 172 && rawBytes[1] >= 16 && rawBytes[1] <= 31 ||
            rawBytes[0] == 192 && rawBytes[1] == 168;
    }

    private static bool LooksLikeOpenAiChatCompletion(JsonElement root)
    {
        return root.TryGetProperty("choices", out var choices) &&
            choices.ValueKind == JsonValueKind.Array &&
            choices.GetArrayLength() > 0 &&
            choices[0].TryGetProperty("message", out var message) &&
            message.TryGetProperty("content", out _);
    }

    private static bool LooksLikeOpenAiStreamDelta(JsonElement root)
    {
        return root.TryGetProperty("choices", out var choices) &&
            choices.ValueKind == JsonValueKind.Array &&
            choices.GetArrayLength() > 0;
    }

    private static string ExtractMessageContent(JsonElement root)
    {
        if (root.TryGetProperty("choices", out var choices) &&
            choices.ValueKind == JsonValueKind.Array &&
            choices.GetArrayLength() > 0 &&
            choices[0].TryGetProperty("message", out var message) &&
            message.TryGetProperty("content", out var content) &&
            content.ValueKind == JsonValueKind.String)
        {
            return content.GetString() ?? string.Empty;
        }

        return string.Empty;
    }

    private static string ExtractDeltaContent(JsonElement root)
    {
        if (root.TryGetProperty("choices", out var choices) &&
            choices.ValueKind == JsonValueKind.Array &&
            choices.GetArrayLength() > 0 &&
            choices[0].TryGetProperty("delta", out var delta) &&
            delta.TryGetProperty("content", out var content) &&
            content.ValueKind == JsonValueKind.String)
        {
            return content.GetString() ?? string.Empty;
        }

        return string.Empty;
    }

    private static TokenUsage? ExtractUsage(JsonElement root)
    {
        if (!root.TryGetProperty("usage", out var usage) || usage.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return null;
        }

        return new TokenUsage(
            TryGetInt(usage, "prompt_tokens") ?? TryGetInt(usage, "input_tokens"),
            TryGetInt(usage, "completion_tokens") ?? TryGetInt(usage, "output_tokens"),
            TryGetInt(usage, "total_tokens"));
    }

    private static bool TryParseProbeJson(string content, out JsonElement root)
    {
        root = default;
        var normalized = NormalizeJsonContent(content);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(normalized);
            root = document.RootElement.Clone();
            return root.ValueKind == JsonValueKind.Object;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static string NormalizeJsonContent(string content)
    {
        var trimmed = content.Trim();
        if (trimmed.StartsWith("```", StringComparison.Ordinal))
        {
            var firstNewLine = trimmed.IndexOf('\n');
            var lastFence = trimmed.LastIndexOf("```", StringComparison.Ordinal);
            if (firstNewLine >= 0 && lastFence > firstNewLine)
            {
                trimmed = trimmed[(firstNewLine + 1)..lastFence].Trim();
            }
        }

        var firstBrace = trimmed.IndexOf('{');
        var lastBrace = trimmed.LastIndexOf('}');
        return firstBrace >= 0 && lastBrace > firstBrace
            ? trimmed[firstBrace..(lastBrace + 1)]
            : trimmed;
    }

    private static string TryGetString(JsonElement root, string propertyName)
    {
        return root.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? string.Empty
            : string.Empty;
    }

    private static int? TryGetInt(JsonElement root, string propertyName)
    {
        if (root.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var result))
        {
            return result;
        }

        return null;
    }

    private static string GetExpectedModelFamily(string modelName)
    {
        var normalized = modelName.ToLowerInvariant();
        if (normalized.Contains("gpt", StringComparison.OrdinalIgnoreCase))
        {
            return "gpt";
        }

        if (normalized.Contains("claude", StringComparison.OrdinalIgnoreCase))
        {
            return "claude";
        }

        if (normalized.Contains("gemini", StringComparison.OrdinalIgnoreCase))
        {
            return "gemini";
        }

        return "unknown";
    }

    private sealed record TokenUsage(int? InputTokens, int? OutputTokens, int? TotalTokens);

    private sealed record ProbeResponse(
        int? FirstTokenMs,
        int FullResponseMs,
        string Content,
        TokenUsage? Usage,
        bool OpenAiShapeValid,
        bool? StreamIntegrityValid,
        int? StreamChunkCount,
        bool? StreamDoneSeen);
}
