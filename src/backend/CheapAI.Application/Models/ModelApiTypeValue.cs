namespace CheapAI.Application.Models;

public static class ModelApiTypeValue
{
    public const string OpenAi = "openai";
    public const string Anthropic = "anthropic";

    public static readonly HashSet<string> All = [OpenAi, Anthropic];
}
