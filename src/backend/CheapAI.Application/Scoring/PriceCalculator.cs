namespace CheapAI.Application.Scoring;

public static class PriceCalculator
{
    public static decimal? CalculateEffectiveUsd(decimal? sitePriceUsd, decimal rechargeRatio, decimal bonusRatio)
    {
        if (!sitePriceUsd.HasValue)
        {
            return null;
        }

        var multiplier = rechargeRatio * (1 + bonusRatio);
        if (multiplier <= 0)
        {
            multiplier = 1;
        }

        return Math.Round(sitePriceUsd.Value / multiplier, 6, MidpointRounding.AwayFromZero);
    }
}
