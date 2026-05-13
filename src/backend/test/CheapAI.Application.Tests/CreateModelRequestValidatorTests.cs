using CheapAI.Application.Models;
using FluentAssertions;

namespace CheapAI.Application.Tests;

public sealed class CreateModelRequestValidatorTests
{
    [Fact]
    public void Validate_ShouldFail_WhenStatusIsInvalid()
    {
        var validator = new CreateModelRequestValidator();
        var result = validator.Validate(new CreateModelRequest
        {
            Vendor = "OpenAI",
            OfficialModelId = "gpt-4.1-mini",
            DisplayName = "GPT-4.1 mini",
            Status = "broken"
        });

        result.IsValid.Should().BeFalse();
    }
}
