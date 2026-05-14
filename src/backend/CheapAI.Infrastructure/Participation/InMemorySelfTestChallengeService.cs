using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using CheapAI.Application.Participation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CheapAI.Infrastructure.Participation;

public sealed class InMemorySelfTestChallengeService(
    IOptions<TurnstileOptions> turnstileOptions,
    ILogger<InMemorySelfTestChallengeService> logger) : ISelfTestChallengeService
{
    private const string TurnstileChallengeId = "cloudflare-turnstile";
    private static readonly TimeSpan ChallengeTtl = TimeSpan.FromMinutes(5);
    private static readonly HttpClient HttpClient = new();
    private readonly ConcurrentDictionary<string, ChallengeEntry> challenges = new();

    public SelfTestChallengeResponse CreateChallenge()
    {
        CleanupExpired();

        var left = RandomNumberGenerator.GetInt32(3, 18);
        var right = RandomNumberGenerator.GetInt32(2, 16);
        var answer = (left + right).ToString();
        var salt = Convert.ToHexString(RandomNumberGenerator.GetBytes(12));
        var id = Guid.NewGuid().ToString("N");
        var expiresAt = DateTime.UtcNow.Add(ChallengeTtl);

        challenges[id] = new ChallengeEntry(HashAnswer(answer, salt), salt, expiresAt);

        return new SelfTestChallengeResponse
        {
            Id = id,
            Question = $"{left} + {right} = ?",
            ExpiresAt = expiresAt
        };
    }

    public async Task<bool> VerifyAndConsumeAsync(string challengeId, string challengeAnswer, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(challengeId) || string.IsNullOrWhiteSpace(challengeAnswer))
        {
            return false;
        }

        var normalizedChallengeId = challengeId.Trim();
        var normalizedAnswer = challengeAnswer.Trim();

        if (string.Equals(normalizedChallengeId, TurnstileChallengeId, StringComparison.OrdinalIgnoreCase))
        {
            return await VerifyTurnstileAsync(normalizedAnswer, cancellationToken);
        }

        if (!challenges.TryRemove(normalizedChallengeId, out var entry) || entry.ExpiresAt <= DateTime.UtcNow)
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(entry.AnswerHash),
            Encoding.UTF8.GetBytes(HashAnswer(normalizedAnswer, entry.Salt)));
    }

    private async Task<bool> VerifyTurnstileAsync(string token, CancellationToken cancellationToken)
    {
        var secretKey = turnstileOptions.Value.SecretKey?.Trim();
        if (string.IsNullOrWhiteSpace(secretKey))
        {
            logger.LogWarning("Turnstile verification failed because no secret key is configured.");
            return false;
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, turnstileOptions.Value.SiteVerifyUrl)
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["secret"] = secretKey,
                ["response"] = token
            })
        };

        try
        {
            using var response = await HttpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Turnstile verification returned HTTP {StatusCode}.", response.StatusCode);
                return false;
            }

            var payload = await response.Content.ReadFromJsonAsync<TurnstileSiteVerifyResponse>(cancellationToken);
            return payload?.Success == true;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Turnstile verification request failed.");
            return false;
        }
    }

    private void CleanupExpired()
    {
        var now = DateTime.UtcNow;
        foreach (var item in challenges)
        {
            if (item.Value.ExpiresAt <= now)
            {
                challenges.TryRemove(item.Key, out _);
            }
        }
    }

    private static string HashAnswer(string answer, string salt)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{salt}:{answer}"));
        return Convert.ToHexString(bytes);
    }

    private sealed record ChallengeEntry(string AnswerHash, string Salt, DateTime ExpiresAt);

    private sealed class TurnstileSiteVerifyResponse
    {
        public bool Success { get; init; }
    }
}
