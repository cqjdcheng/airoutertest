using CheapAI.Application.RelaySites;
using FluentAssertions;

namespace CheapAI.Application.Tests;

public sealed class CreateRelaySiteRequestValidatorTests
{
    [Fact]
    public void Validate_ShouldFail_WhenNameIsMissing()
    {
        var validator = new CreateRelaySiteRequestValidator();
        var result = validator.Validate(new CreateRelaySiteRequest
        {
            BaseUrl = "https://api.example.com"
        });

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_ShouldPass_WhenRequiredFieldsArePresent()
    {
        var validator = new CreateRelaySiteRequestValidator();
        var result = validator.Validate(new CreateRelaySiteRequest
        {
            Name = "Relay Port",
            BaseUrl = "https://api.example.com"
        });

        result.IsValid.Should().BeTrue();
    }
}
