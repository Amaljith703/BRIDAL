using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace MakeupArt.IntegrationTests;

public sealed class ApiHealthTests
{
    [Fact]
    public async Task HealthEndpointReturnsTheRunningApiVersion()
    {
        using var factory = new TestApiFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/health");

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("application/json", response.Content.Headers.ContentType?.MediaType);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"status\":\"ok\"", body);
        Assert.Contains("\"version\":\"1.0.0.0\"", body);
    }

    private sealed class TestApiFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = "Server=127.0.0.1;Database=MakeupArtTests;User Id=sa;Password=NotARealPassword123!;TrustServerCertificate=True",
                    ["Jwt:Issuer"] = "MakeupArtTests",
                    ["Jwt:Audience"] = "MakeupArtTests",
                    ["Jwt:SigningKey"] = new string('t', 64)
                }));
        }
    }
}
