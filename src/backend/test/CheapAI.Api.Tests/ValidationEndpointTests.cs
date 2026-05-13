using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CheapAI.Api.Tests;

public sealed class ValidationEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ValidationEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(_ => { });
    }

    [Fact]
    public async Task CreateSelfTest_ShouldReturnBadRequest_WhenRequestIsInvalid()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/public/self-tests", new
        {
            siteUrl = "not-a-url",
            modelName = "",
            apiKey = "",
            isStream = true,
            testMode = "basic"
        });
        var content = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        content.Should().Contain("Validation failed");
    }
}
