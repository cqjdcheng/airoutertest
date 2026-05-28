using CheapAI.Application.Participation;
using CheapAI.Infrastructure.Participation;

var token = Environment.GetEnvironmentVariable("ANTHROPIC_AUTH_TOKEN");
if (string.IsNullOrWhiteSpace(token))
{
    Console.Error.WriteLine("ANTHROPIC_AUTH_TOKEN is required.");
    return 2;
}

var runner = new OpenAiCompatibleSelfTestRunner();
var result = await runner.ExecuteAsync(new CreateSelfTestRequest
{
    SiteUrl = "https://socheap.ai",
    ModelName = "claude-opus-4-7",
    ApiKey = token,
    ApiType = "anthropic",
    IsStream = true,
    TestMode = "comprehensive"
});

Console.WriteLine($"status={result.Status}");
Console.WriteLine($"matchScore={result.MatchScore}");
Console.WriteLine($"riskScore={result.RiskScore}");
Console.WriteLine($"firstTokenMs={result.FirstTokenMs}");
Console.WriteLine($"fullResponseMs={result.FullResponseMs}");
Console.WriteLine($"summary={result.ResultSummary}");
foreach (var check in result.Checks)
{
    Console.WriteLine($"{check.Code}:{check.Status}:{check.Confidence}:risk={check.RiskImpact}");
}

return result.Status == "succeeded" ? 0 : 1;
