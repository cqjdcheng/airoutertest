using System.Text.Json;
using CheapAI.Application.Common.Exceptions;
using CheapAI.Application.Scoring;

namespace CheapAI.Application.RelaySites;

public sealed class RelayPricingParser
{
    public IReadOnlyList<RelayPricingPreviewItemResponse> Parse(
        string providerType,
        string primaryJson,
        string? extraJson,
        RelayPricingPreviewRequest request)
    {
        try
        {
            using var primaryDocument = JsonDocument.Parse(primaryJson);
            using var extraDocument = string.IsNullOrWhiteSpace(extraJson) ? null : JsonDocument.Parse(extraJson);

            var normalizedType = string.IsNullOrWhiteSpace(providerType)
                ? RelayPricingProviderType.Auto
                : providerType.Trim().ToLowerInvariant();

            return normalizedType switch
            {
                RelayPricingProviderType.NewApi => ParseNewApi(primaryDocument.RootElement, request),
                RelayPricingProviderType.OneApi => ParseOneApi(primaryDocument.RootElement, request),
                RelayPricingProviderType.OneHub => ParseOneHub(primaryDocument.RootElement, extraDocument?.RootElement, request),
                RelayPricingProviderType.Auto => ParseAuto(primaryDocument.RootElement, extraDocument?.RootElement, request),
                _ => throw new AppConflictException("不支持的价格源类型")
            };
        }
        catch (JsonException)
        {
            throw new AppConflictException("价格接口返回不是有效 JSON");
        }
    }

    public static bool CanParseNewApi(JsonElement root)
    {
        return IsSuccess(root) &&
            root.TryGetProperty("data", out var data) &&
            data.ValueKind == JsonValueKind.Array &&
            root.TryGetProperty("group_ratio", out var groupRatio) &&
            groupRatio.ValueKind == JsonValueKind.Object;
    }

    public static bool CanParseOneApi(JsonElement root)
    {
        return IsSuccess(root) &&
            root.TryGetProperty("data", out var data) &&
            data.ValueKind == JsonValueKind.Object &&
            data.TryGetProperty("model_group", out var modelGroup) &&
            modelGroup.ValueKind == JsonValueKind.Object &&
            data.TryGetProperty("model_completion_ratio", out var completionRatio) &&
            completionRatio.ValueKind == JsonValueKind.Object &&
            data.TryGetProperty("group_special", out var groupSpecial) &&
            groupSpecial.ValueKind == JsonValueKind.Object;
    }

    public static bool CanParseOneHub(JsonElement root)
    {
        return IsSuccess(root) &&
            root.TryGetProperty("data", out var data) &&
            data.ValueKind == JsonValueKind.Object;
    }

    private IReadOnlyList<RelayPricingPreviewItemResponse> ParseAuto(
        JsonElement root,
        JsonElement? extra,
        RelayPricingPreviewRequest request)
    {
        if (CanParseNewApi(root))
        {
            return ParseNewApi(root, request);
        }

        if (CanParseOneApi(root))
        {
            return ParseOneApi(root, request);
        }

        if (CanParseOneHub(root))
        {
            return ParseOneHub(root, extra, request);
        }

        throw new AppConflictException("价格接口返回格式无法识别，请手动选择 NewAPI、OneAPI 或 OneHub");
    }

    private IReadOnlyList<RelayPricingPreviewItemResponse> ParseNewApi(JsonElement root, RelayPricingPreviewRequest request)
    {
        if (!CanParseNewApi(root))
        {
            throw new AppConflictException("NewAPI 价格接口返回格式不正确");
        }

        var groups = ReadGroupRates(root.GetProperty("group_ratio"));
        var fallbackGroups = ReadStringArray(root, "auto_groups");
        var items = new List<RelayPricingPreviewItemResponse>();

        foreach (var item in root.GetProperty("data").EnumerateArray())
        {
            var modelName = TryGetString(item, "model_name");
            if (string.IsNullOrWhiteSpace(modelName))
            {
                continue;
            }

            var supportedGroups = ReadStringArray(item, "enable_groups");
            if (supportedGroups.Count == 0)
            {
                supportedGroups = fallbackGroups.Count > 0 ? fallbackGroups : groups.Keys.ToList();
            }

            if (!TryGetDecimal(item, "model_ratio", out var modelRate))
            {
                continue;
            }

            var completionRatio = TryGetDecimal(item, "completion_ratio", out var ratio) ? ratio : 1;
            var group = SelectGroup(groups, supportedGroups, request.GroupId);
            if (group is null)
            {
                continue;
            }

            items.Add(CreateTokenItem(modelName, modelRate, completionRatio, group, request));
        }

        return Deduplicate(items);
    }

    private IReadOnlyList<RelayPricingPreviewItemResponse> ParseOneApi(JsonElement root, RelayPricingPreviewRequest request)
    {
        if (!CanParseOneApi(root))
        {
            throw new AppConflictException("OneAPI 价格接口返回格式不正确");
        }

        var data = root.GetProperty("data");
        var modelGroup = data.GetProperty("model_group");
        var completionRatios = data.GetProperty("model_completion_ratio");
        var groupSpecial = data.GetProperty("group_special");
        var groups = new Dictionary<string, RelayPricingGroup>(StringComparer.OrdinalIgnoreCase);
        var modelPricing = new Dictionary<string, List<OneApiModelPrice>>(StringComparer.OrdinalIgnoreCase);

        foreach (var groupProperty in modelGroup.EnumerateObject())
        {
            var groupId = groupProperty.Name;
            var groupRate = TryGetDecimal(groupProperty.Value, "GroupRatio", out var parsedRate) ? parsedRate : 1;
            groups[groupId] = new RelayPricingGroup(groupId, groupId, groupRate);

            if (!groupProperty.Value.TryGetProperty("ModelPrice", out var prices) || prices.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            foreach (var priceProperty in prices.EnumerateObject())
            {
                if (!TryGetDecimal(priceProperty.Value, "price", out var price))
                {
                    continue;
                }

                var isPerCall = TryGetBool(priceProperty.Value, "isPrice");
                if (!modelPricing.TryGetValue(priceProperty.Name, out var rows))
                {
                    rows = [];
                    modelPricing[priceProperty.Name] = rows;
                }

                rows.Add(new OneApiModelPrice(groupId, price, isPerCall));
            }
        }

        var items = new List<RelayPricingPreviewItemResponse>();
        foreach (var (modelName, prices) in modelPricing)
        {
            var supportedGroups = ReadStringArray(groupSpecial, modelName);
            if (supportedGroups.Count == 0)
            {
                supportedGroups = prices.Select(x => x.GroupId).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            }

            var relevantPrices = prices
                .Where(x => supportedGroups.Contains(x.GroupId, StringComparer.OrdinalIgnoreCase))
                .ToList();
            if (relevantPrices.Count == 0)
            {
                continue;
            }

            var first = relevantPrices[0];
            if (relevantPrices.Any(x => x.IsPerCall != first.IsPerCall || x.Price != first.Price))
            {
                continue;
            }

            var completionRatio = TryGetDecimal(completionRatios, modelName, out var ratio) ? ratio : 1;
            var group = SelectGroup(groups, supportedGroups, request.GroupId);
            if (group is null)
            {
                continue;
            }

            items.Add(first.IsPerCall
                ? CreatePerCallItem(modelName, first.Price, completionRatio, group, request, applyRateBaseline: false)
                : CreateTokenItem(modelName, first.Price, completionRatio, group, request));
        }

        return Deduplicate(items);
    }

    private IReadOnlyList<RelayPricingPreviewItemResponse> ParseOneHub(
        JsonElement root,
        JsonElement? extra,
        RelayPricingPreviewRequest request)
    {
        if (!CanParseOneHub(root))
        {
            throw new AppConflictException("OneHub 模型价格接口返回格式不正确");
        }

        if (extra is null || !IsSuccess(extra.Value) || !extra.Value.TryGetProperty("data", out var groupMap))
        {
            throw new AppConflictException("OneHub 价格抓取需要同时获取用户分组映射");
        }

        var groups = new Dictionary<string, RelayPricingGroup>(StringComparer.OrdinalIgnoreCase);
        foreach (var groupProperty in groupMap.EnumerateObject())
        {
            var groupId = groupProperty.Name;
            var groupName = TryGetString(groupProperty.Value, "name") ?? groupId;
            var groupRate = TryGetDecimal(groupProperty.Value, "ratio", out var parsedRate) ? parsedRate : 1;
            groups[groupId] = new RelayPricingGroup(groupId, groupName, groupRate);
        }

        var items = new List<RelayPricingPreviewItemResponse>();
        foreach (var modelProperty in root.GetProperty("data").EnumerateObject())
        {
            var modelName = modelProperty.Name;
            var supportedGroups = ReadStringArray(modelProperty.Value, "groups");
            if (!modelProperty.Value.TryGetProperty("price", out var price) || price.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            if (!TryGetDecimal(price, "input", out var inputRate))
            {
                continue;
            }

            var outputRate = TryGetDecimal(price, "output", out var parsedOutput) ? parsedOutput : inputRate;
            var completionRatio = inputRate == 0 ? 1 : outputRate / inputRate;
            var billingType = TryGetString(price, "type") ?? RelayPricingBillingType.Tokens;
            var group = SelectGroup(groups, supportedGroups, request.GroupId);
            if (group is null)
            {
                continue;
            }

            items.Add(billingType.Equals(RelayPricingBillingType.Times, StringComparison.OrdinalIgnoreCase)
                ? CreatePerCallItem(modelName, inputRate + outputRate, completionRatio, group, request, applyRateBaseline: true)
                : CreateTokenItem(modelName, inputRate, completionRatio, group, request));
        }

        return Deduplicate(items);
    }

    private static RelayPricingPreviewItemResponse CreateTokenItem(
        string modelName,
        decimal modelRate,
        decimal completionRatio,
        RelayPricingGroup group,
        RelayPricingPreviewRequest request)
    {
        var siteInputPrice = RoundPrice(request.RateBaseline * modelRate * group.Rate * 1000m);
        var siteOutputPrice = RoundPrice(request.RateBaseline * modelRate * completionRatio * group.Rate * 1000m);

        return new RelayPricingPreviewItemResponse
        {
            OfficialModelId = modelName,
            RequestName = ResolveRequestName(modelName),
            ApiType = ResolveApiType(modelName),
            DisplayName = modelName,
            BillingType = RelayPricingBillingType.Tokens,
            GroupId = group.Id,
            GroupName = group.Name,
            GroupRate = group.Rate,
            ModelRate = modelRate,
            CompletionRatio = completionRatio,
            SiteInputPriceUsd = siteInputPrice,
            SiteOutputPriceUsd = siteOutputPrice,
            EffectiveInputPriceUsd = PriceCalculator.CalculateEffectiveUsd(siteInputPrice, request.RechargeRatio, request.BonusRatio),
            EffectiveOutputPriceUsd = PriceCalculator.CalculateEffectiveUsd(siteOutputPrice, request.RechargeRatio, request.BonusRatio),
            RechargeRatio = request.RechargeRatio,
            BonusRatio = request.BonusRatio
        };
    }

    private static RelayPricingPreviewItemResponse CreatePerCallItem(
        string modelName,
        decimal perPrice,
        decimal completionRatio,
        RelayPricingGroup group,
        RelayPricingPreviewRequest request,
        bool applyRateBaseline)
    {
        var baseline = applyRateBaseline ? request.RateBaseline : 1;
        var sitePerCallPrice = RoundPrice(baseline * perPrice * group.Rate);

        return new RelayPricingPreviewItemResponse
        {
            OfficialModelId = modelName,
            RequestName = ResolveRequestName(modelName),
            ApiType = ResolveApiType(modelName),
            DisplayName = modelName,
            BillingType = RelayPricingBillingType.Times,
            GroupId = group.Id,
            GroupName = group.Name,
            GroupRate = group.Rate,
            ModelRate = perPrice,
            CompletionRatio = completionRatio,
            SitePerCallPriceUsd = sitePerCallPrice,
            EffectivePerCallPriceUsd = PriceCalculator.CalculateEffectiveUsd(sitePerCallPrice, request.RechargeRatio, request.BonusRatio),
            RechargeRatio = request.RechargeRatio,
            BonusRatio = request.BonusRatio
        };
    }

    private static RelayPricingGroup? SelectGroup(
        IReadOnlyDictionary<string, RelayPricingGroup> groups,
        IReadOnlyList<string> supportedGroupIds,
        string? requestedGroupId)
    {
        if (!string.IsNullOrWhiteSpace(requestedGroupId))
        {
            var requested = requestedGroupId.Trim();
            var matched = groups.Values.FirstOrDefault(x =>
                x.Id.Equals(requested, StringComparison.OrdinalIgnoreCase) ||
                x.Name.Equals(requested, StringComparison.OrdinalIgnoreCase));

            if (matched is null || !supportedGroupIds.Contains(matched.Id, StringComparer.OrdinalIgnoreCase))
            {
                throw new AppConflictException("指定用户分组不在模型支持范围内");
            }

            return matched;
        }

        return supportedGroupIds
            .Select(id => groups.GetValueOrDefault(id))
            .Where(group => group is not null)
            .OrderBy(group => group!.Rate)
            .ThenBy(group => group!.Id, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
    }

    private static Dictionary<string, RelayPricingGroup> ReadGroupRates(JsonElement value)
    {
        var groups = new Dictionary<string, RelayPricingGroup>(StringComparer.OrdinalIgnoreCase);
        foreach (var groupProperty in value.EnumerateObject())
        {
            if (TryGetDecimal(groupProperty.Value, out var rate))
            {
                groups[groupProperty.Name] = new RelayPricingGroup(groupProperty.Name, groupProperty.Name, rate);
            }
        }

        return groups;
    }

    private static List<string> ReadStringArray(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return property.EnumerateArray()
            .Where(item => item.ValueKind == JsonValueKind.String)
            .Select(item => item.GetString() ?? string.Empty)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string? TryGetString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }

    private static bool TryGetDecimal(JsonElement element, string propertyName, out decimal value)
    {
        value = 0;
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return false;
        }

        return TryGetDecimal(property, out value);
    }

    private static bool TryGetDecimal(JsonElement element, out decimal value)
    {
        value = 0;
        if (element.ValueKind == JsonValueKind.Number && element.TryGetDecimal(out value))
        {
            return true;
        }

        return element.ValueKind == JsonValueKind.String &&
            decimal.TryParse(element.GetString(), out value);
    }

    private static bool TryGetBool(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var value) && value.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Number => value.TryGetInt32(out var number) && number != 0,
            _ => false
        };
    }

    private static bool IsSuccess(JsonElement root)
    {
        return root.TryGetProperty("success", out var success) && success.ValueKind == JsonValueKind.True;
    }

    private static IReadOnlyList<RelayPricingPreviewItemResponse> Deduplicate(IEnumerable<RelayPricingPreviewItemResponse> items)
    {
        return items
            .Where(item => !string.IsNullOrWhiteSpace(item.OfficialModelId))
            .GroupBy(item => item.OfficialModelId, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderBy(item => item.OfficialModelId, StringComparer.OrdinalIgnoreCase)
            .Take(500)
            .ToList();
    }

    private static decimal RoundPrice(decimal value)
    {
        return Math.Round(value, 6, MidpointRounding.AwayFromZero);
    }

    private static string ResolveRequestName(string modelName)
    {
        var normalized = modelName.Trim();
        if (normalized.Contains('/', StringComparison.Ordinal))
        {
            normalized = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries).LastOrDefault() ?? normalized;
        }

        return normalized.TrimStart('~');
    }

    private static string ResolveApiType(string modelName)
    {
        return modelName.Contains("anthropic", StringComparison.OrdinalIgnoreCase) ||
            modelName.Contains("claude", StringComparison.OrdinalIgnoreCase)
                ? "anthropic"
                : "openai";
    }

    private sealed record RelayPricingGroup(string Id, string Name, decimal Rate);

    private sealed record OneApiModelPrice(string GroupId, decimal Price, bool IsPerCall);
}
