using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using CheapAI.Application.Participation;

namespace CheapAI.Infrastructure.Participation;

public sealed class InMemorySelfTestChallengeService : ISelfTestChallengeService
{
    private static readonly TimeSpan ChallengeTtl = TimeSpan.FromMinutes(5);
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

    public bool VerifyAndConsume(string challengeId, string challengeAnswer)
    {
        if (string.IsNullOrWhiteSpace(challengeId) || string.IsNullOrWhiteSpace(challengeAnswer))
        {
            return false;
        }

        if (!challenges.TryRemove(challengeId.Trim(), out var entry) || entry.ExpiresAt <= DateTime.UtcNow)
        {
            return false;
        }

        var normalizedAnswer = challengeAnswer.Trim();
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(entry.AnswerHash),
            Encoding.UTF8.GetBytes(HashAnswer(normalizedAnswer, entry.Salt)));
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
}
