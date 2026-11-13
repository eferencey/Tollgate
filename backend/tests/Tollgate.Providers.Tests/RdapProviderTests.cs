// RDAP provider against a fake network and a fixed clock: registration date -> age, lookup candidates, no-data cases.
using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Tollgate.Providers.Rdap;

namespace Tollgate.Providers.Tests;

public class RdapProviderTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 1, 12, 0, 0, TimeSpan.Zero);

    private sealed class FixedClock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => Now;
    }

    private static RdapProvider CreateProvider(StubHttpHandler handler) =>
        new(new HttpClient(handler),
            Options.Create(new RdapOptions { BaseAddress = new Uri("https://rdap.test/") }),
            new FixedClock(),
            NullLogger<RdapProvider>.Instance);

    private static string RegisteredOn(DateTimeOffset date) =>
        $$"""{"events":[{"eventAction":"last changed","eventDate":"2026-05-01T00:00:00Z"},{"eventAction":"registration","eventDate":"{{date:yyyy-MM-ddTHH:mm:ssZ}}"}]}""";

    [Fact]
    public async Task Registration_event_becomes_the_domain_age_in_days()
    {
        var handler = new StubHttpHandler(_ => StubHttpHandler.Json(RegisteredOn(Now.AddDays(-100))));

        var result = await CreateProvider(handler).CheckAsync(new Uri("https://example.com/"), CancellationToken.None);

        Assert.Equal(ProviderStatus.Completed, result.Status);
        Assert.Equal(100, result.Evidence!.DomainAgeDays);
    }

    [Fact]
    public async Task Registration_date_in_the_future_is_clamped_to_zero()
    {
        var handler = new StubHttpHandler(_ => StubHttpHandler.Json(RegisteredOn(Now.AddDays(2))));

        var result = await CreateProvider(handler).CheckAsync(new Uri("https://example.com/"), CancellationToken.None);

        Assert.Equal(0, result.Evidence!.DomainAgeDays);
    }

    [Fact]
    public async Task Answer_without_a_registration_event_is_completed_with_unknown_age()
    {
        var handler = new StubHttpHandler(_ => StubHttpHandler.Json("""{"events":[{"eventAction":"last changed","eventDate":"2026-05-01T00:00:00Z"}]}"""));

        var result = await CreateProvider(handler).CheckAsync(new Uri("https://example.com/"), CancellationToken.None);

        Assert.Equal(ProviderStatus.Completed, result.Status);
        Assert.Null(result.Evidence!.DomainAgeDays);
    }

    [Fact]
    public async Task Subdomain_falls_back_to_the_registered_domain_after_a_404()
    {
        var handler = new StubHttpHandler(request => request.Path == "/domain/example.com"
            ? StubHttpHandler.Json(RegisteredOn(Now.AddDays(-400)))
            : StubHttpHandler.Status(HttpStatusCode.NotFound));

        var result = await CreateProvider(handler).CheckAsync(new Uri("https://www.example.com/x"), CancellationToken.None);

        Assert.Equal(400, result.Evidence!.DomainAgeDays);
        Assert.Equal(["/domain/www.example.com", "/domain/example.com"], handler.Requests.Select(r => r.Path));
    }

    [Fact]
    public async Task Two_part_public_suffix_is_found_before_the_suffix_itself()
    {
        var handler = new StubHttpHandler(request => request.Path == "/domain/example.co.uk"
            ? StubHttpHandler.Json(RegisteredOn(Now.AddDays(-30)))
            : StubHttpHandler.Status(HttpStatusCode.NotFound));

        var result = await CreateProvider(handler).CheckAsync(new Uri("https://shop.example.co.uk/"), CancellationToken.None);

        Assert.Equal(30, result.Evidence!.DomainAgeDays);
        Assert.Equal("/domain/example.co.uk", Assert.Single(handler.Requests).Path);
    }

    [Fact]
    public async Task Hostname_with_many_labels_causes_at_most_two_lookups()
    {
        var handler = new StubHttpHandler(_ => StubHttpHandler.Status(HttpStatusCode.NotFound));

        await CreateProvider(handler).CheckAsync(new Uri("https://a.b.c.d.e.f.g.h.example.com/"), CancellationToken.None);

        Assert.Equal(["/domain/h.example.com", "/domain/example.com"], handler.Requests.Select(r => r.Path));
    }

    [Fact]
    public async Task Domain_unknown_to_rdap_is_a_completed_answer_with_unknown_age()
    {
        var handler = new StubHttpHandler(_ => StubHttpHandler.Status(HttpStatusCode.NotFound));

        var result = await CreateProvider(handler).CheckAsync(new Uri("https://example.com.tr/"), CancellationToken.None);

        Assert.Equal(ProviderStatus.Completed, result.Status);
        Assert.Null(result.Evidence!.DomainAgeDays);
    }

    [Theory]
    [InlineData("http://203.0.113.7/login")]
    [InlineData("http://[2001:db8::1]/login")]
    [InlineData("http://localhost/")]
    public async Task Ip_addresses_and_single_label_hosts_are_unavailable_without_any_request(string url)
    {
        var handler = new StubHttpHandler(_ => StubHttpHandler.Json("{}"));

        var result = await CreateProvider(handler).CheckAsync(new Uri(url), CancellationToken.None);

        Assert.Equal(ProviderStatus.Unavailable, result.Status);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task Internationalized_domain_is_queried_in_its_ascii_form()
    {
        var handler = new StubHttpHandler(_ => StubHttpHandler.Json(RegisteredOn(Now.AddDays(-5))));

        await CreateProvider(handler).CheckAsync(new Uri("https://bücher.example/"), CancellationToken.None);

        Assert.Equal("/domain/xn--bcher-kva.example", Assert.Single(handler.Requests).Path);
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.TooManyRequests)]
    public async Task Http_error_is_unavailable(HttpStatusCode status)
    {
        var handler = new StubHttpHandler(_ => StubHttpHandler.Status(status));

        var result = await CreateProvider(handler).CheckAsync(new Uri("https://example.com/"), CancellationToken.None);

        Assert.Equal(ProviderStatus.Unavailable, result.Status);
    }

    [Fact]
    public async Task Malformed_json_is_unavailable()
    {
        var handler = new StubHttpHandler(_ => StubHttpHandler.Json("not json"));

        var result = await CreateProvider(handler).CheckAsync(new Uri("https://example.com/"), CancellationToken.None);

        Assert.Equal(ProviderStatus.Unavailable, result.Status);
    }
}
