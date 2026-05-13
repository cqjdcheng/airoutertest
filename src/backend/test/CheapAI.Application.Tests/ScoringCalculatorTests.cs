using CheapAI.Application.Scoring;
using FluentAssertions;

namespace CheapAI.Application.Tests;

public sealed class ScoringCalculatorTests
{
    [Fact]
    public void CalculateEffectiveUsd_ShouldApplyRechargeAndBonusMultiplier()
    {
        var result = PriceCalculator.CalculateEffectiveUsd(
            sitePriceUsd: 1.2m,
            rechargeRatio: 2m,
            bonusRatio: 0.5m);

        result.Should().Be(0.4m);
    }

    [Fact]
    public void CalculateEffectiveUsd_ShouldReturnNull_WhenSitePriceIsMissing()
    {
        var result = PriceCalculator.CalculateEffectiveUsd(
            sitePriceUsd: null,
            rechargeRatio: 2m,
            bonusRatio: 0.5m);

        result.Should().BeNull();
    }

    [Theory]
    [InlineData("success", 500, 2_000, 14)]
    [InlineData("failed", 500, 2_000, 45)]
    [InlineData("success", 3_001, 2_000, 20)]
    [InlineData("success", 500, 15_001, 20)]
    [InlineData("failed", 3_001, 15_001, 85)]
    public void ResolveRiskScore_ShouldAccumulateRuleHits(
        string status,
        int firstTokenMs,
        int fullResponseMs,
        decimal expected)
    {
        var result = RiskScoreCalculator.ResolveRiskScore(status, firstTokenMs, fullResponseMs);

        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(10, "low")]
    [InlineData(21, "medium")]
    [InlineData(51, "high")]
    [InlineData(81, "critical")]
    public void ResolveRiskLevel_ShouldMapScoreBuckets(decimal score, string expected)
    {
        var result = RiskScoreCalculator.ResolveRiskLevel(score);

        result.Should().Be(expected);
    }
}
