using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using CheapAI.Application.Common.Exceptions;
using CheapAI.Application.RelaySites;

namespace CheapAI.Infrastructure.RelaySites;

public sealed class OneTrackerRelayPricingCrawler : IRelayPricingCrawler
{
    private const string PricingPath = "/api/pricing";
    private const string OneHubModelsPath = "/api/available_model";
    private const string OneHubGroupsPath = "/api/user_group_map";

    private static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(12)
    };

    private readonly RelayPricingParser parser = new();

    public async Task<IReadOnlyList<RelayPricingPreviewItemResponse>> PreviewAsync(
        RelayPricingPreviewRequest request,
        CancellationToken cancellationToken = default)
    {
        var providerType = string.IsNullOrWhiteSpace(request.ProviderType)
            ? RelayPricingProviderType.Auto
            : request.ProviderType.Trim().ToLowerInvariant();

        return providerType switch
        {
            RelayPricingProviderType.NewApi or RelayPricingProviderType.OneApi => await FetchPricingAsync(providerType, request, cancellationToken),
            RelayPricingProviderType.OneHub => await FetchOneHubAsync(request, cancellationToken),
            RelayPricingProviderType.Auto => await FetchAutoAsync(request, cancellationToken),
            _ => throw new AppConflictException("不支持的价格源类型")
        };
    }

    private async Task<IReadOnlyList<RelayPricingPreviewItemResponse>> FetchAutoAsync(
        RelayPricingPreviewRequest request,
        CancellationToken cancellationToken)
    {
        var pricingEndpoint = BuildEndpoint(request.BaseUrl, PricingPath);
        await EnsurePublicEndpointAsync(pricingEndpoint, cancellationToken);
        var pricingBody = await TryFetchAsync(pricingEndpoint, request.ApiKey, cancellationToken);

        if (!string.IsNullOrWhiteSpace(pricingBody) && CanParsePricingEndpoint(pricingBody))
        {
            return parser.Parse(RelayPricingProviderType.Auto, pricingBody, null, request);
        }

        return await FetchOneHubAsync(request, cancellationToken);
    }

    private async Task<IReadOnlyList<RelayPricingPreviewItemResponse>> FetchPricingAsync(
        string providerType,
        RelayPricingPreviewRequest request,
        CancellationToken cancellationToken)
    {
        var endpoint = BuildEndpoint(request.BaseUrl, PricingPath);
        await EnsurePublicEndpointAsync(endpoint, cancellationToken);
        var body = await FetchRequiredAsync(endpoint, request.ApiKey, "价格接口", cancellationToken);
        return parser.Parse(providerType, body, null, request);
    }

    private async Task<IReadOnlyList<RelayPricingPreviewItemResponse>> FetchOneHubAsync(
        RelayPricingPreviewRequest request,
        CancellationToken cancellationToken)
    {
        var modelsEndpoint = BuildEndpoint(request.BaseUrl, OneHubModelsPath);
        var groupsEndpoint = BuildEndpoint(request.BaseUrl, OneHubGroupsPath);
        await EnsurePublicEndpointAsync(modelsEndpoint, cancellationToken);
        await EnsurePublicEndpointAsync(groupsEndpoint, cancellationToken);

        var modelsBody = await FetchRequiredAsync(modelsEndpoint, request.ApiKey, "OneHub 模型价格接口", cancellationToken);
        var groupsBody = await FetchRequiredAsync(groupsEndpoint, request.ApiKey, "OneHub 用户分组接口", cancellationToken);
        return parser.Parse(RelayPricingProviderType.OneHub, modelsBody, groupsBody, request);
    }

    private static async Task<string?> TryFetchAsync(Uri endpoint, string? apiKey, CancellationToken cancellationToken)
    {
        try
        {
            return await FetchRequiredAsync(endpoint, apiKey, "价格接口", cancellationToken);
        }
        catch (AppConflictException)
        {
            return null;
        }
    }

    private static async Task<string> FetchRequiredAsync(
        Uri endpoint,
        string? apiKey,
        string endpointName,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
        request.Headers.UserAgent.ParseAdd("CheapAI/1.0 one-tracker-compatible");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey.Trim());
        }

        try
        {
            using var response = await HttpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw new AppConflictException($"{endpointName}抓取失败，HTTP {(int)response.StatusCode}");
            }

            return await response.Content.ReadAsStringAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new AppConflictException($"{endpointName}抓取超时");
        }
        catch (HttpRequestException exception)
        {
            throw new AppConflictException($"{endpointName}抓取失败：{exception.Message}");
        }
        catch (JsonException)
        {
            throw new AppConflictException($"{endpointName}返回不是有效 JSON");
        }
    }

    private static bool CanParsePricingEndpoint(string body)
    {
        try
        {
            using var document = JsonDocument.Parse(body);
            return RelayPricingParser.CanParseNewApi(document.RootElement) ||
                RelayPricingParser.CanParseOneApi(document.RootElement);
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static Uri BuildEndpoint(string rawUrl, string path)
    {
        if (!Uri.TryCreate(rawUrl.Trim(), UriKind.Absolute, out var uri) ||
            uri.Scheme is not ("http" or "https"))
        {
            throw new AppConflictException("价格抓取地址必须是完整的 http 或 https URL");
        }

        return new Uri(uri.ToString().TrimEnd('/') + path);
    }

    private static async Task EnsurePublicEndpointAsync(Uri endpoint, CancellationToken cancellationToken)
    {
        if (IsReservedHost(endpoint.Host))
        {
            throw new AppConflictException("示例域名不能用于真实价格抓取");
        }

        if (IsBlockedHostName(endpoint.Host))
        {
            throw new AppConflictException("价格抓取地址不能指向本机、内网或 metadata 网络");
        }

        var addresses = await Dns.GetHostAddressesAsync(endpoint.Host, cancellationToken);
        if (addresses.Any(IsBlockedIpAddress))
        {
            throw new AppConflictException("价格抓取地址解析到了本机、内网或 metadata 网络");
        }
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
        if (IPAddress.IsLoopback(ipAddress) ||
            ipAddress.IsIPv6LinkLocal ||
            ipAddress.IsIPv6SiteLocal ||
            ipAddress.Equals(IPAddress.Any) ||
            ipAddress.Equals(IPAddress.IPv6Any) ||
            ipAddress.Equals(IPAddress.IPv6None))
        {
            return true;
        }

        var rawBytes = ipAddress.GetAddressBytes();
        if (rawBytes.Length == 16)
        {
            return (rawBytes[0] & 0xfe) == 0xfc;
        }

        return rawBytes[0] == 0 ||
            rawBytes[0] == 10 ||
            rawBytes[0] == 127 ||
            rawBytes[0] == 169 && rawBytes[1] == 254 ||
            rawBytes[0] == 172 && rawBytes[1] >= 16 && rawBytes[1] <= 31 ||
            rawBytes[0] == 192 && rawBytes[1] == 168;
    }
}
