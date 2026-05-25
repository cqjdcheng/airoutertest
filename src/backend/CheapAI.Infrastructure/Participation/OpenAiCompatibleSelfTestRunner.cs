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
        Timeout = TimeSpan.FromSeconds(45)
    };

    public async Task<SelfTestExecutionResult> ExecuteAsync(CreateSelfTestRequest request, CancellationToken cancellationToken = default)
    {
        var apiType = NormalizeApiType(request.ApiType, request.ModelName);
        if (!TryBuildEndpoint(request.SiteUrl, apiType, out var endpoint, out var error))
        {
            return Failed(75, "high", error, [
                Probe("D1", "协议连通性", "协议", "fail", "high", 0, 40, error)
            ]);
        }

        if (IsReservedHost(endpoint.Host))
        {
            return Failed(30, "medium", "测试地址是示例保留域名，本地自动化不会真实请求；真实中转站会发起实际请求。", [
                Probe("D1", "协议连通性", "协议", "warn", "high", 8, 12, "示例保留域名不会发起外部请求。")
            ]);
        }

        if (IsBlockedNetworkHost(endpoint.Host))
        {
            return Failed(90, "high", "测试地址指向本机、内网或 metadata 网络，已被安全策略拦截。", [
                Probe("D1", "协议连通性", "协议", "fail", "high", 0, 55, "目标地址属于本机、内网、链路本地或 metadata 网络，已被安全策略拦截。")
            ]);
        }

        if (string.IsNullOrWhiteSpace(request.ApiKey))
        {
            return Failed(70, "high", "需要填写 API Key 才能发起真实模型请求。", [
                Probe("D1", "协议连通性", "协议", "fail", "high", 0, 40, "未填写 API Key，无法发起真实模型请求。")
            ]);
        }

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(45));

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint);
        ApplyAuthHeaders(httpRequest, apiType, request.ApiKey);
        httpRequest.Content = new StringContent(JsonSerializer.Serialize(BuildProbePayload(request, apiType), JsonOptions), Encoding.UTF8, "application/json");

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

            var probeResponse = await ReadResponseAsync(response, apiType, stopwatch, responseHeaderMs, request.IsStream, timeoutCts.Token);

            return EvaluateProbeResponse(request, apiType, endpoint, response, probeResponse);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            stopwatch.Stop();
            return Failed(55, "medium", "请求超时，中转可能不可达、过慢或不稳定。", [
                Probe("D1", "协议连通性", "协议", "fail", "high", 0, 30, "请求超过 45 秒未完成，判定为链路超时。"),
                Probe("D8", "响应时延", "性能", "fail", "high", 0, 25, "完整响应超过超时时间，接口可能不可达、过慢或不稳定。")
            ], fullResponseMs: (int)Math.Min(stopwatch.ElapsedMilliseconds, int.MaxValue));
        }
        catch (HttpRequestException exception)
        {
            stopwatch.Stop();
            return Failed(50, "medium", $"请求失败：{exception.Message}", [
                Probe("D1", "协议连通性", "协议", "fail", "high", 0, 30, "HTTP 请求异常，未能完成真实请求。"),
                Probe("S4", "错误响应泄露", "安全", "unknown", "low", 0, 0, "CheapAI 未保存或展示中转返回的错误正文。")
            ], fullResponseMs: (int)Math.Min(stopwatch.ElapsedMilliseconds, int.MaxValue));
        }
        catch (JsonException exception)
        {
            stopwatch.Stop();
            return Failed(65, "high", $"响应解析失败：{exception.Message}", [
                Probe("D2", "响应结构", "协议", "fail", "high", 0, 35, "响应不是有效的 JSON 或 SSE JSON，无法按目标协议解析。"),
                Probe("D5", "内容完整性", "完整性", "fail", "high", 0, 20, "响应解析失败，因此无法读取模型正文。")
            ], fullResponseMs: (int)Math.Min(stopwatch.ElapsedMilliseconds, int.MaxValue));
        }
    }

    private static Dictionary<string, object?> BuildProbePayload(CreateSelfTestRequest request, string apiType)
    {
        var modelName = NormalizeModelNameForApi(request.ModelName, apiType);
        var prompt = """
                    CheapAI relay verification probe. Return strict JSON only, without Markdown fences:
                    {
                      "model_claim": "the model identity you claim",
                      "knowledge_answer": "answer only 9.11 or 9.8: which number is larger?",
                      "protocol_ack": "openai-chat-completions-compatible",
                      "format_ack": "json-ok",
                      "short_answer": "OK"
                    }
                    """;

        if (apiType == "anthropic")
        {
            return new Dictionary<string, object?>
            {
                ["model"] = modelName,
                ["messages"] = new object[]
                {
                    new
                    {
                        role = "user",
                        content = prompt
                    }
                },
                ["max_tokens"] = 260,
                ["stream"] = request.IsStream
            };
        }

        var payload = new Dictionary<string, object?>
        {
            ["model"] = modelName,
            ["messages"] = new object[]
            {
                new
                {
                    role = "user",
                    content = prompt
                }
            },
            ["max_tokens"] = 260,
            ["stream"] = request.IsStream
        };

        if (!ShouldOmitSamplingParameters(request.ModelName, apiType))
        {
            payload["temperature"] = 0;
        }

        if (request.IsStream)
        {
            payload["stream_options"] = new { include_usage = true };
        }

        return payload;
    }

    private static async Task<ProbeResponse> ReadResponseAsync(
        HttpResponseMessage response,
        string apiType,
        Stopwatch stopwatch,
        int responseHeaderMs,
        bool requestedStream,
        CancellationToken cancellationToken)
    {
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        stopwatch.Stop();

        if (requestedStream && TryParseSseProbeResponse(body, apiType, responseHeaderMs, (int)Math.Min(stopwatch.ElapsedMilliseconds, int.MaxValue), out var streamResponse))
        {
            return streamResponse;
        }

        return ParseJsonProbeResponse(body, apiType, responseHeaderMs, (int)Math.Min(stopwatch.ElapsedMilliseconds, int.MaxValue));
    }

    private static ProbeResponse ParseJsonProbeResponse(string body, string apiType, int responseHeaderMs, int fullResponseMs)
    {
        using var document = JsonDocument.Parse(body);
        var root = document.RootElement.Clone();
        var content = ExtractMessageContent(root, apiType);
        var usage = ExtractUsage(root, apiType);
        var returnedModel = ExtractReturnedModel(root, apiType);
        var responseShape = ResolveResponseShape(root, apiType, isStream: false);

        return new ProbeResponse(
            responseHeaderMs,
            fullResponseMs,
            content,
            usage,
            returnedModel,
            OpenAiShapeValid: LooksLikeSuccessResponse(root, apiType),
            ResponseShape: responseShape,
            StreamIntegrityValid: null,
            StreamChunkCount: null,
            StreamDoneSeen: null);
    }

    private static bool TryParseSseProbeResponse(
        string body,
        string apiType,
        int responseHeaderMs,
        int fullResponseMs,
        out ProbeResponse probeResponse)
    {
        int? firstTokenMs = null;
        var chunkCount = 0;
        var doneSeen = false;
        var shapeValid = true;
        var contentBuilder = new StringBuilder();
        TokenUsage? usage = null;
        string? returnedModel = null;
        string responseShape = "unknown";

        using var reader = new StringReader(body);
        while (reader.ReadLine() is { } line)
        {
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
            var currentShape = ResolveResponseShape(root, apiType, isStream: true);
            if (currentShape != "unknown")
            {
                responseShape = currentShape;
            }

            if (apiType == "anthropic" &&
                root.TryGetProperty("type", out var eventType) &&
                eventType.ValueKind == JsonValueKind.String &&
                eventType.GetString()?.Equals("message_stop", StringComparison.OrdinalIgnoreCase) == true)
            {
                doneSeen = true;
            }

            if (apiType == "openai" &&
                root.TryGetProperty("type", out var openAiEventType) &&
                openAiEventType.ValueKind == JsonValueKind.String &&
                openAiEventType.GetString()?.Equals("response.completed", StringComparison.OrdinalIgnoreCase) == true)
            {
                doneSeen = true;
            }

            if (!LooksLikeStreamDelta(root, apiType))
            {
                shapeValid = false;
                continue;
            }

            var delta = ExtractDeltaContent(root, apiType);
            returnedModel ??= ExtractReturnedModel(root, apiType);
            if (!string.IsNullOrEmpty(delta))
            {
                firstTokenMs ??= responseHeaderMs;
                contentBuilder.Append(delta);
            }

            usage = MergeUsage(usage, ExtractUsage(root, apiType));
            chunkCount++;
        }

        if (chunkCount == 0)
        {
            probeResponse = null!;
            return false;
        }

        var streamIntegrityValid = doneSeen || HasSseDataLines(body);

        probeResponse = new ProbeResponse(
            firstTokenMs ?? responseHeaderMs,
            fullResponseMs,
            contentBuilder.ToString(),
            usage,
            returnedModel,
            OpenAiShapeValid: shapeValid && chunkCount > 0,
            ResponseShape: responseShape,
            StreamIntegrityValid: streamIntegrityValid,
            StreamChunkCount: chunkCount,
            StreamDoneSeen: doneSeen);
        return true;
    }

    private static SelfTestExecutionResult EvaluateProbeResponse(
        CreateSelfTestRequest request,
        string apiType,
        Uri endpoint,
        HttpResponseMessage httpResponse,
        ProbeResponse response)
    {
        var checks = new List<SelfTestProbeResult>
        {
            Probe("D1", "协议连通性", "协议", "pass", "high", 15, 0, $"真实请求已连通：HTTP {(int)httpResponse.StatusCode}，目标主机 {endpoint.Host}。"),
            Probe("D2", "响应结构", "协议", response.OpenAiShapeValid ? "pass" : "warn", "high", response.OpenAiShapeValid ? 20 : 0, response.OpenAiShapeValid ? 0 : 35, BuildResponseShapeEvidence(apiType, response.OpenAiShapeValid, response.ResponseShape))
        };

        checks.Add(BuildContentIntegrityProbe(response.Content));
        checks.Add(BuildStructuredOutputProbe(response.Content));
        checks.Add(BuildKnowledgeProbe(response.Content));
        checks.Add(BuildLatencyProbe(response.FirstTokenMs, response.FullResponseMs));
        checks.Add(BuildTokenProbe(response.Usage));
        checks.Add(BuildIdentityProbe(request.ModelName, response.Content));
        checks.Add(BuildFullModelProbe(request.ModelName, response.ReturnedModel, response.Content));
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
                    ? $"流式响应完整：收到 {response.StreamChunkCount} 个数据片段，结束标记={response.StreamDoneSeen}。"
                    : $"流式响应已完成，但结束信号不够完整：数据片段 {response.StreamChunkCount} 个，结束标记={response.StreamDoneSeen}。"));
        }
        else
        {
            checks.Add(Probe("S5", "流完整性", "安全", "unknown", "high", 0, 0, "本次使用非流式请求，未检测 SSE 流完整性。"));
        }

        var matchScore = Math.Clamp(checks.Sum(x => x.ScoreImpact), 0, 100);
        var riskScore = Math.Clamp(100 - matchScore + checks.Sum(x => x.RiskImpact), 0, 100);
        var status = "succeeded";
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
            return Probe("D5", "内容完整性", "完整性", "warn", "high", 0, 25, "模型返回内容为空，无法继续判断回答质量。");
        }

        if (content.Length < 8)
        {
            return Probe("D5", "内容完整性", "完整性", "warn", "medium", 5, 8, "模型返回内容过短，可能是中转截断、降级响应或模型没有按要求输出。");
        }

        return Probe("D5", "内容完整性", "完整性", "pass", "high", 10, 0, "模型返回了非空正文，可以继续检查内容和结构。");
    }

    private static SelfTestProbeResult BuildStructuredOutputProbe(string content)
    {
        return TryParseProbeJson(content, out _)
            ? Probe("D7", "结构化输出", "能力", "pass", "high", 15, 0, "模型按要求返回了可解析的严格 JSON。")
            : Probe("D7", "结构化输出", "能力", "warn", "high", 0, 22, "模型没有按要求返回严格 JSON，可能影响自动化解析，但不代表接口请求失败。");
    }

    private static SelfTestProbeResult BuildKnowledgeProbe(string content)
    {
        if (!TryParseProbeJson(content, out var root))
        {
            return Probe("D4", "知识能力", "能力", "unknown", "medium", 0, 0, "由于结构化 JSON 解析失败，无法可靠读取知识题答案。");
        }

        var answer = TryGetString(root, "knowledge_answer").ToLowerInvariant();
        if (answer.Contains("9.8") || answer.Contains("9.80"))
        {
            return Probe("D4", "知识能力", "能力", "pass", "high", 20, 0, "确定性数字比较题回答正确。");
        }

        if (answer.Contains("9.11"))
        {
            return Probe("D4", "知识能力", "能力", "warn", "high", 0, 30, "确定性数字比较题回答错误，说明模型能力或响应内容存在异常。");
        }

        return Probe("D4", "知识能力", "能力", "warn", "medium", 8, 8, "知识题答案不够明确，无法给出满分判断。");
    }

    private static SelfTestProbeResult BuildLatencyProbe(int? firstTokenMs, int fullResponseMs)
    {
        if (firstTokenMs is null)
        {
            return Probe("D8", "响应时延", "性能", "warn", "medium", 4, 8, $"未测到首 token 时间，完整响应耗时 {fullResponseMs}ms。");
        }

        if (firstTokenMs <= 3000 && fullResponseMs <= 12000)
        {
            return Probe("D8", "响应时延", "性能", "pass", "high", 10, 0, $"响应速度正常：首 token {firstTokenMs}ms，完整响应 {fullResponseMs}ms。");
        }

        if (firstTokenMs <= 15000 && fullResponseMs <= 20000)
        {
            return Probe("D8", "响应时延", "性能", "warn", "medium", 5, 8, $"响应偏慢但已完成：首 token {firstTokenMs}ms，完整响应 {fullResponseMs}ms。");
        }

        return Probe("D8", "响应时延", "性能", "warn", "high", 0, 20, $"响应明显偏慢：首 token {firstTokenMs}ms，完整响应 {fullResponseMs}ms。接口可用，但稳定性需要观察。");
    }

    private static SelfTestProbeResult BuildTokenProbe(TokenUsage? usage)
    {
        if (usage is null || usage.TotalTokens is null)
        {
            return Probe("S1", "Token 用量", "安全", "unknown", "medium", 0, 0, "响应中没有返回 token 用量，无法判断是否存在异常消耗。");
        }

        var totalTokens = usage.TotalTokens.Value;
        if (totalTokens > EstimatedTokenLimit * 1.2m)
        {
            return Probe("S1", "Token 用量", "安全", "warn", "medium", 0, 18, $"本次返回 total_tokens={totalTokens}，高于预估 {EstimatedTokenLimit}，需要关注是否存在额外注入或计费异常。");
        }

        return Probe("S1", "Token 用量", "安全", "pass", "medium", 0, 0, $"本次返回 total_tokens={totalTokens}，处于预估范围内。");
    }

    private static SelfTestProbeResult BuildIdentityProbe(string modelName, string content)
    {
        if (!TryParseProbeJson(content, out var root))
        {
            return Probe("D3", "身份一致性", "身份", "unknown", "low", 0, 0, "没有读取到模型自述身份，不能据此判断是否冒充。");
        }

        var claim = TryGetString(root, "model_claim").ToLowerInvariant();
        var expectedFamily = GetExpectedModelFamily(modelName);
        if (string.IsNullOrWhiteSpace(claim) || expectedFamily == "unknown")
        {
            return Probe("D3", "身份一致性", "身份", "unknown", "low", 0, 0, "模型自述身份信息不足，或目标模型族无法识别，只作为低权重参考。");
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
            matched ? $"模型自述身份与 {expectedFamily} 模型族基本一致。" : $"模型自述身份没有明确匹配请求的 {expectedFamily} 模型族。");
    }

    private static SelfTestProbeResult BuildFullModelProbe(string requestedModel, string? returnedModel, string content)
    {
        var requested = NormalizeModelFingerprint(requestedModel);
        var returned = NormalizeModelFingerprint(returnedModel ?? string.Empty);
        var hasReturnedModel = !string.IsNullOrWhiteSpace(returned);
        var exactMatch = hasReturnedModel &&
            (returned == requested ||
             returned.Contains(requested, StringComparison.OrdinalIgnoreCase) ||
             requested.Contains(returned, StringComparison.OrdinalIgnoreCase));
        var claimMatches = TryParseProbeJson(content, out var root) &&
            NormalizeModelFingerprint(TryGetString(root, "model_claim"))
                .Contains(GetExpectedModelFamily(requestedModel), StringComparison.OrdinalIgnoreCase);

        if (exactMatch)
        {
            return Probe("D9", "满血模型指纹", "身份", "pass", "high", 12, 0, $"上游响应 model 字段为 {returnedModel}，与请求模型 {requestedModel} 基本一致。这是判断是否被降级或替换的最高权重信号。");
        }

        if (hasReturnedModel)
        {
            return Probe("D9", "满血模型指纹", "身份", "warn", "high", 0, 28, $"上游响应 model 字段为 {returnedModel}，与请求模型 {requestedModel} 不一致，存在模型别名、降级或替换风险。");
        }

        return Probe(
            "D9",
            "满血模型指纹",
            "身份",
            claimMatches ? "warn" : "unknown",
            claimMatches ? "medium" : "low",
            claimMatches ? 4 : 0,
            claimMatches ? 6 : 0,
            claimMatches
                ? "上游未返回可比对的 model 字段，只能依赖模型自述身份，不能证明是满血模型。"
                : "上游未返回可比对的 model 字段，也没有足够的模型自述信息，无法判断是否为满血模型。");
    }

    private static SelfTestProbeResult BuildUpstreamFingerprintProbe(HttpResponseMessage response)
    {
        var server = response.Headers.Server.ToString();
        var contentType = response.Content.Headers.ContentType?.MediaType ?? "unknown";
        var hasServerHeader = !string.IsNullOrWhiteSpace(server);
        var hasContentType = !contentType.Equals("unknown", StringComparison.OrdinalIgnoreCase);
        var evidence = hasServerHeader
            ? $"已捕获弱指纹：content-type={contentType}，响应包含 server 头。该项只能作为辅助信息，不能单独证明上游身份。"
            : $"已捕获弱指纹：content-type={contentType}，响应未暴露 server 头。该项只能作为辅助信息，不能单独证明上游身份。";

        return hasServerHeader || hasContentType
            ? Probe("D6", "上游指纹", "信息", "pass", "low", 3, 0, evidence)
            : Probe("D6", "上游指纹", "信息", "unknown", "low", 0, 0, "响应头没有可用指纹信息，因此不参与判断。");
    }

    private static SelfTestExecutionResult FromHttpFailure(HttpStatusCode statusCode, int firstTokenMs, int fullResponseMs, HttpResponseMessage response)
    {
        var numericStatus = (int)statusCode;
        var isAuthFailure = statusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden;
        var checks = new List<SelfTestProbeResult>
        {
            Probe("D1", "协议连通性", "协议", "fail", "high", 0, isAuthFailure ? 18 : 35, $"中转返回 HTTP {numericStatus}，真实请求未通过。"),
            Probe("D2", "响应结构", "协议", "unknown", "medium", 0, 0, "没有成功响应可检查结构。"),
            Probe("S4", "错误响应泄露", "安全", "unknown", "low", 0, 0, "错误正文不会保存或展示，只记录 HTTP 状态和摘要。")
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
                ? $"中转返回 {numericStatus}。请检查 API Key、模型权限或账户余额。"
                : $"中转返回 HTTP {numericStatus}，请求链路未通过。",
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
        if (status == "succeeded")
        {
            var attentionCodes = failed.Concat(warned).Distinct().ToArray();
            return attentionCodes.Length == 0
                ? $"真实请求已完成，分数 {matchScore:0.#}，分数越高表示结果越可信。API Key 未落库，自助测试不进入公共排行。"
                : $"真实请求已完成，分数 {matchScore:0.#}，分数越高表示结果越可信。需要人工关注的检测项：{string.Join(", ", attentionCodes)}，这些项目会影响分数，但不代表接口请求失败。";
        }

        return failed.Length == 0
            ? $"请求未完成，分数 {matchScore:0.#}。"
            : $"请求未完成，分数 {matchScore:0.#}，失败项：{string.Join(", ", failed)}。";
    }

    private static string BuildResponseShapeEvidence(string apiType, bool isValid, string responseShape)
    {
        if (apiType == "anthropic")
        {
            return isValid
                ? responseShape == "anthropic_stream"
                    ? "Anthropic Messages 流式响应结构有效：检测到 SSE 事件和文本增量。这里检查的是 Claude/Anthropic 协议，不是 OpenAI choices。"
                    : "Anthropic Messages 响应结构有效：检测到 content 数组和文本内容。这里检查的是 Claude/Anthropic 协议，不是 OpenAI choices。"
                : "Anthropic Messages 响应结构不完整：未检测到预期的 content 数组、文本内容或流式事件。";
        }

        if (!isValid)
        {
            return "OpenAI 响应结构不完整：未检测到 Responses API 的 output/output_text/response 事件，也未检测到 Chat Completions 的 choices/message/delta。";
        }

        return responseShape switch
        {
            "openai_response_stream" => "OpenAI Responses API 流式响应结构有效：检测到 response.* SSE 事件和文本增量；不再要求 choices/message/delta。",
            "openai_response" => "OpenAI Responses API 响应结构有效：检测到 output/output_text 文本内容；不再要求 choices/message/content。",
            "openai_chat_stream" => "OpenAI Chat Completions 流式响应结构有效：检测到 choices/delta 数据片段。",
            "openai_chat" => "OpenAI Chat Completions 响应结构有效：检测到 choices/message/content。",
            _ => "OpenAI 响应结构有效：已识别为兼容的模型输出结构。"
        };
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

    private static bool TryBuildEndpoint(string rawUrl, string apiType, out Uri endpoint, out string error)
    {
        endpoint = null!;
        error = string.Empty;

        if (!Uri.TryCreate(rawUrl.Trim(), UriKind.Absolute, out var uri) ||
            uri.Scheme is not ("http" or "https"))
        {
            error = "中转地址必须是完整的 http 或 https URL。";
            return false;
        }

        var targetPath = apiType == "anthropic" ? "/messages" : "/chat/completions";
        if (uri.AbsolutePath.EndsWith(targetPath, StringComparison.OrdinalIgnoreCase))
        {
            endpoint = uri;
            return true;
        }

        var baseUrl = uri.ToString().TrimEnd('/');
        var suffix = uri.AbsolutePath.TrimEnd('/').EndsWith("/v1", StringComparison.OrdinalIgnoreCase)
            ? targetPath
            : $"/v1{targetPath}";
        endpoint = new Uri(baseUrl + suffix);
        return true;
    }

    private static string NormalizeApiType(string? apiType, string modelName)
    {
        if (string.Equals(apiType, "anthropic", StringComparison.OrdinalIgnoreCase))
        {
            return "anthropic";
        }

        if (string.Equals(apiType, "openai", StringComparison.OrdinalIgnoreCase))
        {
            return "openai";
        }

        return modelName.Contains("claude", StringComparison.OrdinalIgnoreCase) ||
            modelName.Contains("anthropic", StringComparison.OrdinalIgnoreCase)
                ? "anthropic"
                : "openai";
    }

    private static bool ShouldOmitSamplingParameters(string modelName, string apiType)
    {
        return string.Equals(apiType, "anthropic", StringComparison.OrdinalIgnoreCase) ||
            modelName.Contains("claude", StringComparison.OrdinalIgnoreCase) ||
            modelName.Contains("anthropic", StringComparison.OrdinalIgnoreCase);
    }

    private static void ApplyAuthHeaders(HttpRequestMessage request, string apiType, string apiKey)
    {
        if (apiType == "anthropic")
        {
            request.Headers.TryAddWithoutValidation("x-api-key", apiKey);
            request.Headers.TryAddWithoutValidation("anthropic-version", "2023-06-01");
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            return;
        }

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
    }

    private static string NormalizeModelNameForApi(string modelName, string apiType)
    {
        var normalized = modelName.Trim();
        return apiType == "anthropic" ? normalized.Replace('.', '-') : normalized;
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

    private static bool LooksLikeSuccessResponse(JsonElement root, string apiType)
    {
        return apiType == "anthropic"
            ? LooksLikeAnthropicMessage(root)
            : LooksLikeOpenAiChatCompletion(root) || LooksLikeOpenAiResponse(root);
    }

    private static bool LooksLikeStreamDelta(JsonElement root, string apiType)
    {
        return apiType == "anthropic"
            ? LooksLikeAnthropicStreamEvent(root)
            : LooksLikeOpenAiStreamDelta(root) || LooksLikeOpenAiResponseStreamEvent(root);
    }

    private static string ResolveResponseShape(JsonElement root, string apiType, bool isStream)
    {
        if (apiType == "anthropic")
        {
            if (isStream && LooksLikeAnthropicStreamEvent(root))
            {
                return "anthropic_stream";
            }

            return LooksLikeAnthropicMessage(root) ? "anthropic_message" : "unknown";
        }

        if (isStream && LooksLikeOpenAiResponseStreamEvent(root))
        {
            return "openai_response_stream";
        }

        if (isStream && LooksLikeOpenAiStreamDelta(root))
        {
            return "openai_chat_stream";
        }

        if (LooksLikeOpenAiResponse(root))
        {
            return "openai_response";
        }

        return LooksLikeOpenAiChatCompletion(root) ? "openai_chat" : "unknown";
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

    private static bool LooksLikeOpenAiResponse(JsonElement root)
    {
        if (root.TryGetProperty("output_text", out var outputText) &&
            outputText.ValueKind == JsonValueKind.String)
        {
            return true;
        }

        return root.TryGetProperty("output", out var output) &&
            output.ValueKind == JsonValueKind.Array &&
            output.GetArrayLength() > 0;
    }

    private static bool LooksLikeOpenAiResponseStreamEvent(JsonElement root)
    {
        return root.TryGetProperty("type", out var type) &&
            type.ValueKind == JsonValueKind.String &&
            type.GetString()?.StartsWith("response.", StringComparison.OrdinalIgnoreCase) == true;
    }

    private static bool LooksLikeAnthropicMessage(JsonElement root)
    {
        return root.TryGetProperty("content", out var content) &&
            content.ValueKind == JsonValueKind.Array &&
            content.GetArrayLength() > 0;
    }

    private static bool LooksLikeAnthropicStreamEvent(JsonElement root)
    {
        return root.TryGetProperty("type", out var type) &&
            type.ValueKind == JsonValueKind.String;
    }

    private static string ExtractMessageContent(JsonElement root, string apiType)
    {
        if (apiType == "anthropic")
        {
            return ExtractAnthropicMessageContent(root);
        }

        var responseContent = ExtractOpenAiResponseContent(root);
        if (!string.IsNullOrEmpty(responseContent))
        {
            return responseContent;
        }

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

    private static string ExtractDeltaContent(JsonElement root, string apiType)
    {
        if (apiType == "anthropic")
        {
            return ExtractAnthropicDeltaContent(root);
        }

        var responseDelta = ExtractOpenAiResponseDeltaContent(root);
        if (!string.IsNullOrEmpty(responseDelta))
        {
            return responseDelta;
        }

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

    private static string ExtractOpenAiResponseContent(JsonElement root)
    {
        if (root.TryGetProperty("output_text", out var outputText) &&
            outputText.ValueKind == JsonValueKind.String)
        {
            return outputText.GetString() ?? string.Empty;
        }

        if (!root.TryGetProperty("output", out var output) || output.ValueKind != JsonValueKind.Array)
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        foreach (var outputItem in output.EnumerateArray())
        {
            if (!outputItem.TryGetProperty("content", out var content) || content.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var contentItem in content.EnumerateArray())
            {
                if (contentItem.TryGetProperty("text", out var text) && text.ValueKind == JsonValueKind.String)
                {
                    builder.Append(text.GetString());
                }
            }
        }

        return builder.ToString();
    }

    private static string ExtractOpenAiResponseDeltaContent(JsonElement root)
    {
        if (root.TryGetProperty("delta", out var delta) && delta.ValueKind == JsonValueKind.String)
        {
            return delta.GetString() ?? string.Empty;
        }

        if (root.TryGetProperty("text", out var text) && text.ValueKind == JsonValueKind.String)
        {
            return text.GetString() ?? string.Empty;
        }

        return string.Empty;
    }

    private static string ExtractAnthropicMessageContent(JsonElement root)
    {
        if (!root.TryGetProperty("content", out var content) || content.ValueKind != JsonValueKind.Array)
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        foreach (var item in content.EnumerateArray())
        {
            if (item.TryGetProperty("text", out var text) && text.ValueKind == JsonValueKind.String)
            {
                builder.Append(text.GetString());
            }
        }

        return builder.ToString();
    }

    private static string ExtractAnthropicDeltaContent(JsonElement root)
    {
        if (root.TryGetProperty("delta", out var delta) &&
            delta.TryGetProperty("text", out var text) &&
            text.ValueKind == JsonValueKind.String)
        {
            return text.GetString() ?? string.Empty;
        }

        return string.Empty;
    }

    private static string? ExtractReturnedModel(JsonElement root, string apiType)
    {
        if (root.TryGetProperty("model", out var model) && model.ValueKind == JsonValueKind.String)
        {
            return model.GetString();
        }

        if (apiType == "openai" &&
            root.TryGetProperty("response", out var response) &&
            response.TryGetProperty("model", out var responseModel) &&
            responseModel.ValueKind == JsonValueKind.String)
        {
            return responseModel.GetString();
        }

        if (apiType == "anthropic" &&
            root.TryGetProperty("message", out var message) &&
            message.TryGetProperty("model", out var messageModel) &&
            messageModel.ValueKind == JsonValueKind.String)
        {
            return messageModel.GetString();
        }

        return null;
    }

    private static TokenUsage? ExtractUsage(JsonElement root, string apiType)
    {
        if (apiType == "openai" &&
            root.TryGetProperty("response", out var response) &&
            response.TryGetProperty("usage", out var responseUsage))
        {
            return ExtractOpenAiUsage(responseUsage);
        }

        if (!root.TryGetProperty("usage", out var usage) || usage.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return null;
        }

        if (apiType == "anthropic")
        {
            var input = TryGetInt(usage, "input_tokens");
            var output = TryGetInt(usage, "output_tokens");
            return new TokenUsage(input, output, input.HasValue || output.HasValue ? (input ?? 0) + (output ?? 0) : null);
        }

        return ExtractOpenAiUsage(usage);
    }

    private static TokenUsage ExtractOpenAiUsage(JsonElement usage)
    {
        var input = TryGetInt(usage, "prompt_tokens") ?? TryGetInt(usage, "input_tokens");
        var output = TryGetInt(usage, "completion_tokens") ?? TryGetInt(usage, "output_tokens");
        return new TokenUsage(
            input,
            output,
            TryGetInt(usage, "total_tokens") ?? (input.HasValue || output.HasValue ? (input ?? 0) + (output ?? 0) : null));
    }

    private static bool HasSseDataLines(string body)
    {
        using var reader = new StringReader(body);
        while (reader.ReadLine() is { } line)
        {
            if (line.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static TokenUsage? MergeUsage(TokenUsage? current, TokenUsage? next)
    {
        if (current is null)
        {
            return next;
        }

        if (next is null)
        {
            return current;
        }

        var input = next.InputTokens ?? current.InputTokens;
        var output = next.OutputTokens ?? current.OutputTokens;
        var total = next.TotalTokens ?? current.TotalTokens ?? (input.HasValue || output.HasValue ? (input ?? 0) + (output ?? 0) : null);
        return new TokenUsage(input, output, total);
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

    private static string NormalizeModelFingerprint(string modelName)
    {
        return new string(modelName
            .Trim()
            .ToLowerInvariant()
            .Where(char.IsLetterOrDigit)
            .ToArray());
    }

    private sealed record TokenUsage(int? InputTokens, int? OutputTokens, int? TotalTokens);

    private sealed record ProbeResponse(
        int? FirstTokenMs,
        int FullResponseMs,
        string Content,
        TokenUsage? Usage,
        string? ReturnedModel,
        bool OpenAiShapeValid,
        string ResponseShape,
        bool? StreamIntegrityValid,
        int? StreamChunkCount,
        bool? StreamDoneSeen);
}
