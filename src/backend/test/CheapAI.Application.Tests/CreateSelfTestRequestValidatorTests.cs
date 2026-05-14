using CheapAI.Application.Participation;
using FluentAssertions;

namespace CheapAI.Application.Tests;

public sealed class CreateSelfTestRequestValidatorTests
{
    [Fact]
    public void Validate_ShouldPass_WhenChallengeAnswerLooksLikeToken()
    {
        var validator = new CreateSelfTestRequestValidator();
        var result = validator.Validate(new CreateSelfTestRequest
        {
            SiteUrl = "https://api.example.com",
            ModelName = "gpt-5.5",
            ApiKey = "sk-test",
            TestMode = "comprehensive",
            ChallengeId = "cloudflare-turnstile",
            ChallengeAnswer = "XXXX.DUMMY.TOKEN.XXXX"
        });

        result.IsValid.Should().BeTrue();
    }
}
