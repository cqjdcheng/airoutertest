using CheapAI.Application.RelaySites;
using FluentAssertions;

namespace CheapAI.Application.Tests;

public sealed class SlugHelperTests
{
    [Fact]
    public void Normalize_ShouldUseFallbackName_WhenExplicitSlugIsEmpty()
    {
        var result = SlugHelper.Normalize(string.Empty, "Relay Port");

        result.Should().Be("relay-port");
    }

    [Fact]
    public void Normalize_ShouldCollapseInvalidCharacters()
    {
        var result = SlugHelper.Normalize("GPT 4.1 Mini!!!", "fallback");

        result.Should().Be("gpt-4-1-mini");
    }
}
