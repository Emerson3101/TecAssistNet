using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using TecAssist.IntegrationTests.Infrastructure;

namespace TecAssist.IntegrationTests;

[Collection(ApiCollection.Name)]
public sealed class HealthTests(ApiFactory factory)
{
    [Theory]
    [InlineData("/health")]
    [InlineData("/health/ready")]
    public async Task HealthEndpoints_ReturnHealthy(string path)
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task ApiEndpoints_AreScoped_ToDevUser_WithoutAuth()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/documents");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
