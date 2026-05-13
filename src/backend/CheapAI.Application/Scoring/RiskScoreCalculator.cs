namespace CheapAI.Application.Scoring;

public static class RiskScoreCalculator
{
    public static decimal ResolveRiskScore(string testStatus, int? firstTokenMs, int? fullResponseMs)
    {
        decimal score = 0;

        if (!string.Equals(testStatus, "success", StringComparison.OrdinalIgnoreCase))
        {
            score += 45;
        }

        if (firstTokenMs is > 3000)
        {
            score += 20;
        }

        if (fullResponseMs is > 15000)
        {
            score += 20;
        }

        if (score == 0)
        {
            score = 14;
        }

        return Math.Min(score, 100);
    }

    public static string ResolveRiskLevel(decimal score)
    {
        if (score >= 81) return "critical";
        if (score >= 51) return "high";
        if (score >= 21) return "medium";
        return "low";
    }
}
