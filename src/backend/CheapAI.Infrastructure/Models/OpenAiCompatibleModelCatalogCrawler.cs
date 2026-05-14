using System.Net;
using System.Net.Http.Headers;
using System.Globalization;
using System.Text.Json;
using CheapAI.Application.Common.Exceptions;
using CheapAI.Application.Models;
using CheapAI.Application.RelaySites;

namespace CheapAI.Infrastructure.Models;

public sealed class OpenAiCompatibleModelCatalogCrawler : IModelCatalogCrawler
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly Uri OpenRouterModelsEndpoint = new("https://openrouter.ai/api/v1/models");
    private static readonly IReadOnlyDictionary<string, string> ProviderNameOverrides = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["arcee-ai"] = "Arcee AI",
        ["baidu"] = "Baidu Qianfan",
        ["bytedance-seed"] = "ByteDance Seed",
        ["ibm-granite"] = "IBM",
        ["meta-llama"] = "Meta",
        ["moonshotai"] = "MoonshotAI",
        ["mistralai"] = "Mistral",
        ["nex-agi"] = "Nex AGI",
        ["openai"] = "OpenAI",
        ["openrouter"] = "OpenRouter",
        ["prime-intellect"] = "Prime Intellect",
        ["x-ai"] = "xAI",
        ["z-ai"] = "Z.ai"
    };
    private static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(12)
    };

    public async Task<IReadOnlyList<ModelImportPreviewItemResponse>> PreviewAsync(ModelImportPreviewRequest request, CancellationToken cancellationToken = default)
    {
        var endpoint = BuildModelsEndpoint(request.BaseUrl);
        await EnsurePublicEndpointAsync(endpoint, cancellationToken);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Get, endpoint);
        if (!string.IsNullOrWhiteSpace(request.ApiKey))
        {
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", request.ApiKey.Trim());
        }

        try
        {
            using var response = await HttpClient.SendAsync(httpRequest, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw new AppConflictException($"模型列表抓取失败，HTTP {(int)response.StatusCode}");
            }

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            return ParseModelList(body, request.Vendor.Trim());
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new AppConflictException("模型列表抓取超时");
        }
        catch (HttpRequestException exception)
        {
            throw new AppConflictException($"模型列表抓取失败：{exception.Message}");
        }
        catch (JsonException)
        {
            throw new AppConflictException("模型列表返回不是有效 JSON");
        }
    }

    public async Task<IReadOnlyList<ModelImportPreviewItemResponse>> PreviewOpenRouterAsync(CancellationToken cancellationToken = default)
    {
        using var httpRequest = new HttpRequestMessage(HttpMethod.Get, OpenRouterModelsEndpoint);
        httpRequest.Headers.UserAgent.ParseAdd("CheapAI/1.0");

        try
        {
            using var response = await HttpClient.SendAsync(httpRequest, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw new AppConflictException($"OpenRouter 模型列表抓取失败，HTTP {(int)response.StatusCode}");
            }

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            return ParseOpenRouterModelList(body);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new AppConflictException("OpenRouter 模型列表抓取超时");
        }
        catch (HttpRequestException exception)
        {
            throw new AppConflictException($"OpenRouter 模型列表抓取失败：{exception.Message}");
        }
        catch (JsonException)
        {
            throw new AppConflictException("OpenRouter 模型列表返回不是有效 JSON");
        }
    }

    private static Uri BuildModelsEndpoint(string rawUrl)
    {
        if (!Uri.TryCreate(rawUrl.Trim(), UriKind.Absolute, out var uri) ||
            uri.Scheme is not ("http" or "https"))
        {
            throw new AppConflictException("模型抓取地址必须是完整的 http 或 https URL");
        }

        if (uri.AbsolutePath.EndsWith("/models", StringComparison.OrdinalIgnoreCase))
        {
            return uri;
        }

        var baseUrl = uri.ToString().TrimEnd('/');
        var suffix = uri.AbsolutePath.TrimEnd('/').EndsWith("/v1", StringComparison.OrdinalIgnoreCase)
            ? "/models"
            : "/v1/models";
        return new Uri(baseUrl + suffix);
    }

    private static async Task EnsurePublicEndpointAsync(Uri endpoint, CancellationToken cancellationToken)
    {
        if (IsReservedHost(endpoint.Host))
        {
            throw new AppConflictException("示例域名不能用于真实模型抓取");
        }

        if (IsBlockedHostName(endpoint.Host))
        {
            throw new AppConflictException("模型抓取地址不能指向本机、内网或 metadata 网络");
        }

        var addresses = await Dns.GetHostAddressesAsync(endpoint.Host, cancellationToken);
        if (addresses.Any(IsBlockedIpAddress))
        {
            throw new AppConflictException("模型抓取地址解析到了本机、内网或 metadata 网络");
        }
    }

    private static IReadOnlyList<ModelImportPreviewItemResponse> ParseModelList(string body, string vendor)
    {
        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;
        var rows = root.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Array
            ? data.EnumerateArray()
            : root.ValueKind == JsonValueKind.Array
                ? root.EnumerateArray()
                : throw new AppConflictException("模型列表缺少 data 数组");

        return rows
            .Select(item => MapModel(item, vendor))
            .Where(item => !string.IsNullOrWhiteSpace(item.OfficialModelId))
            .GroupBy(item => item.OfficialModelId, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .Take(200)
            .ToList();
    }

    private static IReadOnlyList<ModelImportPreviewItemResponse> ParseOpenRouterModelList(string body)
    {
        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;
        var rows = root.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Array
            ? data.EnumerateArray()
            : throw new AppConflictException("OpenRouter 模型列表缺少 data 数组");

        return rows
            .Select(MapOpenRouterModel)
            .Where(item => !string.IsNullOrWhiteSpace(item.OfficialModelId))
            .GroupBy(item => item.OfficialModelId, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderByDescending(item => item.CapabilityScore ?? 0)
            .ThenBy(item => item.ProviderName)
            .ThenBy(item => item.DisplayName)
            .Take(200)
            .ToList();
    }

    private static ModelImportPreviewItemResponse MapModel(JsonElement item, string vendor)
    {
        var modelId = TryGetString(item, "id") ?? TryGetString(item, "model") ?? TryGetString(item, "name") ?? string.Empty;
        var displayName = TryGetString(item, "display_name") ?? TryGetString(item, "displayName") ?? TryGetString(item, "name") ?? modelId;
        var resolvedVendor = TryGetString(item, "owned_by") ?? TryGetString(item, "vendor") ?? vendor;

        return new ModelImportPreviewItemResponse
        {
            ProviderSlug = SlugHelper.Normalize(null, string.IsNullOrWhiteSpace(resolvedVendor) ? vendor : resolvedVendor),
            ProviderName = string.IsNullOrWhiteSpace(resolvedVendor) ? vendor : resolvedVendor,
            Vendor = string.IsNullOrWhiteSpace(resolvedVendor) ? vendor : resolvedVendor,
            OfficialModelId = modelId,
            DisplayName = displayName,
            Description = TryGetString(item, "description"),
            OfficialInputPriceUsd = TryGetPrice(item, "official_input_price_usd", "input_price", "prompt_price", "price_input"),
            OfficialOutputPriceUsd = TryGetPrice(item, "official_output_price_usd", "output_price", "completion_price", "price_output")
        };
    }

    private static ModelImportPreviewItemResponse MapOpenRouterModel(JsonElement item)
    {
        var modelId = TryGetString(item, "id") ?? string.Empty;
        var rawDisplayName = TryGetString(item, "name") ?? modelId;
        var providerSlug = ResolveOpenRouterProviderSlug(modelId);
        var providerName = ResolveOpenRouterProviderName(providerSlug, rawDisplayName);
        var displayName = StripOpenRouterProviderPrefix(rawDisplayName, providerName, providerSlug);
        var capabilityScore = CalculateOpenRouterCapabilityScore(item);

        return new ModelImportPreviewItemResponse
        {
            ProviderSlug = providerSlug,
            ProviderName = providerName,
            Vendor = providerName,
            OfficialModelId = modelId,
            DisplayName = displayName,
            Description = TryGetString(item, "description"),
            OfficialInputPriceUsd = TryGetOpenRouterPricePerMillion(item, "prompt"),
            OfficialOutputPriceUsd = TryGetOpenRouterPricePerMillion(item, "completion"),
            CapabilityScore = capabilityScore,
            CapabilitySource = "OpenRouter"
        };
    }

    private static string? TryGetString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }

    private static string ResolveOpenRouterProviderSlug(string modelId)
    {
        var rawProvider = modelId.Split('/', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "openrouter";
        return SlugHelper.Normalize(null, rawProvider);
    }

    private static string ResolveOpenRouterProviderName(string providerSlug, string displayName)
    {
        var colonIndex = displayName.IndexOf(':', StringComparison.Ordinal);
        if (colonIndex > 0)
        {
            return displayName[..colonIndex].Trim();
        }

        var textInfo = CultureInfo.InvariantCulture.TextInfo;
        return ProviderNameOverrides.GetValueOrDefault(providerSlug, textInfo.ToTitleCase(providerSlug.Replace('-', ' ')));
    }

    private static string StripOpenRouterProviderPrefix(string displayName, string providerName, string providerSlug)
    {
        var normalizedName = displayName.Trim();
        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            return displayName;
        }

        var candidates = new[]
            {
                providerName,
                ProviderNameOverrides.GetValueOrDefault(providerSlug),
                providerSlug.Replace("-", " "),
                providerSlug.Replace("-", string.Empty)
            }
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase);

        foreach (var candidate in candidates)
        {
            var prefix = candidate;
            if (normalizedName.StartsWith($"{prefix}:", StringComparison.OrdinalIgnoreCase))
            {
                return normalizedName[(prefix.Length + 1)..].Trim();
            }

            if (normalizedName.StartsWith($"{prefix} ", StringComparison.OrdinalIgnoreCase))
            {
                return normalizedName[(prefix.Length + 1)..].Trim();
            }
        }

        return normalizedName;
    }

    private static decimal? TryGetPrice(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            if (TryGetDecimal(element, name, out var value))
            {
                return value;
            }
        }

        if (element.TryGetProperty("pricing", out var pricing) && pricing.ValueKind == JsonValueKind.Object)
        {
            foreach (var name in names)
            {
                if (TryGetDecimal(pricing, name, out var value))
                {
                    return value;
                }
            }
        }

        return null;
    }

    private static decimal? TryGetOpenRouterPricePerMillion(JsonElement item, string propertyName)
    {
        if (!item.TryGetProperty("pricing", out var pricing) || pricing.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        return TryGetDecimal(pricing, propertyName, out var pricePerToken)
            ? Math.Round(pricePerToken * 1_000_000m, 6)
            : null;
    }

    private static decimal CalculateOpenRouterCapabilityScore(JsonElement item)
    {
        var contextLength = TryGetInt(item, "context_length") ?? TryGetNestedInt(item, "top_provider", "context_length") ?? 0;
        var maxCompletionTokens = TryGetNestedInt(item, "top_provider", "max_completion_tokens") ?? 0;
        var score = 50m;

        score += Math.Min(contextLength / 1_000_000m, 1m) * 25m;
        score += Math.Min(maxCompletionTokens / 128_000m, 1m) * 8m;

        var supportedParameters = TryGetStringSet(item, "supported_parameters");
        if (supportedParameters.Contains("tools") || supportedParameters.Contains("tool_choice"))
        {
            score += 5m;
        }

        if (supportedParameters.Contains("reasoning") || supportedParameters.Contains("include_reasoning"))
        {
            score += 5m;
        }

        if (supportedParameters.Contains("structured_outputs") || supportedParameters.Contains("response_format"))
        {
            score += 4m;
        }

        var inputModalities = TryGetNestedStringSet(item, "architecture", "input_modalities");
        if (inputModalities.Contains("image"))
        {
            score += 2m;
        }

        if (inputModalities.Contains("video") || inputModalities.Contains("file"))
        {
            score += 1m;
        }

        return Math.Round(Math.Min(score, 100m), 2);
    }

    private static int? TryGetInt(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        if (property.ValueKind == JsonValueKind.Number && property.TryGetInt32(out var numericValue))
        {
            return numericValue;
        }

        return property.ValueKind == JsonValueKind.String && int.TryParse(property.GetString(), out var stringValue)
            ? stringValue
            : null;
    }

    private static int? TryGetNestedInt(JsonElement element, string objectName, string propertyName)
    {
        return element.TryGetProperty(objectName, out var nested) && nested.ValueKind == JsonValueKind.Object
            ? TryGetInt(nested, propertyName)
            : null;
    }

    private static HashSet<string> TryGetStringSet(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return property.EnumerateArray()
            .Where(item => item.ValueKind == JsonValueKind.String)
            .Select(item => item.GetString() ?? string.Empty)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static HashSet<string> TryGetNestedStringSet(JsonElement element, string objectName, string propertyName)
    {
        return element.TryGetProperty(objectName, out var nested) && nested.ValueKind == JsonValueKind.Object
            ? TryGetStringSet(nested, propertyName)
            : [];
    }

    private static bool TryGetDecimal(JsonElement element, string propertyName, out decimal value)
    {
        value = 0;
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return false;
        }

        if (property.ValueKind == JsonValueKind.Number && property.TryGetDecimal(out value))
        {
            return true;
        }

        return property.ValueKind == JsonValueKind.String &&
            decimal.TryParse(property.GetString(), out value);
    }

    private static bool IsReservedHost(string host)
    {
        return host.EndsWith(".example", StringComparison.OrdinalIgnoreCase) ||
            host.Equals("example.com", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsBlockedHostName(string host)
    {
        return host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
            host.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase) ||
            host.EndsWith(".local", StringComparison.OrdinalIgnoreCase) ||
            host.EndsWith(".lan", StringComparison.OrdinalIgnoreCase) ||
            host.EndsWith(".internal", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsBlockedIpAddress(IPAddress ipAddress)
    {
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
}
