using CheapAI.Application.RelaySites;
using FluentAssertions;

namespace CheapAI.Application.Tests;

public sealed class RelayPricingParserTests
{
    private readonly RelayPricingParser parser = new();

    [Fact]
    public void Parse_ShouldCalculateNewApiTokenPricesWithBestGroup()
    {
        const string json = """
        {
          "success": true,
          "auto_groups": [],
          "group_ratio": { "default": 1, "vip": 0.5 },
          "data": [
            {
              "model_name": "gpt-4o",
              "model_ratio": 5,
              "completion_ratio": 3,
              "enable_groups": ["default", "vip"]
            }
          ]
        }
        """;

        var result = parser.Parse(RelayPricingProviderType.NewApi, json, null, new RelayPricingPreviewRequest
        {
            RateBaseline = 0.002m,
            RechargeRatio = 2m,
            BonusRatio = 0.5m
        });

        result.Should().ContainSingle();
        var item = result[0];
        item.GroupId.Should().Be("vip");
        item.SiteInputPriceUsd.Should().Be(5m);
        item.SiteOutputPriceUsd.Should().Be(15m);
        item.EffectiveInputPriceUsd.Should().Be(1.666667m);
        item.EffectiveOutputPriceUsd.Should().Be(5m);
    }

    [Fact]
    public void Parse_ShouldCalculateOneApiTokenPricesWithSelectedGroup()
    {
        const string json = """
        {
          "success": true,
          "data": {
            "model_completion_ratio": { "claude-3-5-sonnet": 5 },
            "group_special": { "claude-3-5-sonnet": ["default", "vip"] },
            "model_group": {
              "default": {
                "DisplayName": "default",
                "GroupRatio": 1,
                "ModelPrice": { "claude-3-5-sonnet": { "isPrice": false, "price": 4 } }
              },
              "vip": {
                "DisplayName": "vip",
                "GroupRatio": 0.8,
                "ModelPrice": { "claude-3-5-sonnet": { "isPrice": false, "price": 4 } }
              }
            }
          }
        }
        """;

        var result = parser.Parse(RelayPricingProviderType.OneApi, json, null, new RelayPricingPreviewRequest
        {
            GroupId = "default",
            RateBaseline = 0.002m,
            RechargeRatio = 1m
        });

        result.Should().ContainSingle();
        result[0].GroupId.Should().Be("default");
        result[0].SiteInputPriceUsd.Should().Be(8m);
        result[0].SiteOutputPriceUsd.Should().Be(40m);
    }

    [Fact]
    public void Parse_ShouldCalculateOneHubTokenPricesWithGroupMap()
    {
        const string modelsJson = """
        {
          "success": true,
          "data": {
            "gemini-pro": {
              "groups": ["default", "vip"],
              "owned_by": "google",
              "price": {
                "model": "gemini-pro",
                "type": "tokens",
                "channel_type": 1,
                "input": 2,
                "output": 6,
                "locked": false
              }
            }
          }
        }
        """;
        const string groupJson = """
        {
          "success": true,
          "data": {
            "default": { "id": 1, "symbol": "default", "name": "Default", "ratio": 1, "api_rate": 1, "public": true, "enable": true },
            "vip": { "id": 2, "symbol": "vip", "name": "VIP", "ratio": 0.25, "api_rate": 1, "public": true, "enable": true }
          }
        }
        """;

        var result = parser.Parse(RelayPricingProviderType.OneHub, modelsJson, groupJson, new RelayPricingPreviewRequest
        {
            RateBaseline = 0.002m,
            RechargeRatio = 1m
        });

        result.Should().ContainSingle();
        result[0].GroupId.Should().Be("vip");
        result[0].SiteInputPriceUsd.Should().Be(1m);
        result[0].SiteOutputPriceUsd.Should().Be(3m);
    }
}
