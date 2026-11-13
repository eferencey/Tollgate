// One error format everywhere: RFC 9457 problem details (application/problem+json) with a stable "code".
using System.Net;
using Microsoft.Extensions.DependencyInjection;
using static Tollgate.Api.Tests.TollgateFactory;

namespace Tollgate.Api.Tests;

public class ErrorFormatTests : IDisposable
{
    private readonly TollgateFactory factory = new();

    public void Dispose() => factory.Dispose();

    [Theory]
    [InlineData("", "empty")]
    [InlineData("ftp://example.com/file", "unsupportedScheme")]
    [InlineData("javascript:alert(1)", "unsupportedScheme")]
    [InlineData("http://", "invalidFormat")]
    public async Task An_invalid_url_is_a_400_problem_with_code_and_reason(string url, string reason)
    {
        using var response = await factory.CreateClient().GetAsync(ScanPath(url));
        var problem = System.Text.Json.JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("invalid_url", problem.GetProperty("code").GetString());
        Assert.Equal(reason, problem.GetProperty("reason").GetString());
        Assert.Equal(400, problem.GetProperty("status").GetInt32());
        Assert.False(string.IsNullOrWhiteSpace(problem.GetProperty("detail").GetString()));
    }

    [Fact]
    public async Task A_missing_url_parameter_is_the_same_kind_of_400()
    {
        var (status, problem) = await GetJsonAsync(factory.CreateClient(), "/api/scan");

        Assert.Equal(400, status);
        Assert.Equal("invalid_url", problem.GetProperty("code").GetString());
    }

    [Fact]
    public async Task An_overlong_url_is_rejected()
    {
        var (status, problem) = await GetJsonAsync(factory.CreateClient(), ScanPath("https://example.com/" + new string('a', 2100)));

        Assert.Equal(400, status);
        Assert.Equal("tooLong", problem.GetProperty("reason").GetString());
    }

    [Fact]
    public async Task An_unknown_route_gets_the_same_problem_shape()
    {
        using var response = await factory.CreateClient().GetAsync("/nope");
        var problem = System.Text.Json.JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("not_found", problem.GetProperty("code").GetString());
    }

    [Fact]
    public async Task An_unexpected_exception_becomes_a_500_problem_without_leaking_details()
    {
        // A clock that throws is a bug the scan flow does not guard against (unlike a broken cache or history).
        using var broken = new TollgateFactory().WithServices(services =>
            services.AddSingleton<TimeProvider, ExplodingClock>());

        using var response = await broken.CreateClient().GetAsync(ScanPath("example.com"));
        var text = await response.Content.ReadAsStringAsync();
        var problem = System.Text.Json.JsonDocument.Parse(text).RootElement;

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("internal_error", problem.GetProperty("code").GetString());
        Assert.DoesNotContain("secret internal detail", text);
    }

    private sealed class ExplodingClock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => throw new InvalidOperationException("secret internal detail");
    }
}
