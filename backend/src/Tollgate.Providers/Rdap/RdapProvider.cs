// RDAP (rdap.org bootstrap): reads the domain's registration date and turns it into an age in days.
// "No usable record" (404 or no registration event) is a valid answer with an unknown age, not a failure.
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tollgate.Domain;
using Tollgate.Domain.Evidence;

namespace Tollgate.Providers.Rdap;

public sealed class RdapProvider(
    HttpClient http,
    IOptions<RdapOptions> options,
    TimeProvider clock,
    ILogger<RdapProvider> logger) : ThreatProviderBase<RdapEvidence>(logger)
{
    private const string RegistrationEvent = "registration";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly RdapOptions settings = options.Value;

    public override ThreatSource Source => ThreatSource.Rdap;

    protected override async Task<ProviderResult<RdapEvidence>> QueryAsync(Uri url, CancellationToken cancellationToken)
    {
        var candidates = RegistrableDomainCandidates.For(url.IdnHost);
        if (candidates.Count == 0)
        {
            return ProviderResult<RdapEvidence>.Unavailable("Host is not a domain name, so it has no RDAP record");
        }

        foreach (var domain in candidates)
        {
            using var response = await http.GetAsync(new Uri(settings.BaseAddress, $"domain/{domain}"), cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                continue;
            }

            response.EnsureSuccessStatusCode();
            var record = await response.Content.ReadFromJsonAsync<RdapResponse>(Json, cancellationToken);
            return ProviderResult<RdapEvidence>.Completed(new RdapEvidence(AgeInDays(record)));
        }

        return ProviderResult<RdapEvidence>.Completed(new RdapEvidence(DomainAgeDays: null));
    }

    private int? AgeInDays(RdapResponse? record)
    {
        var registered = record?.Events?
            .FirstOrDefault(e => e.EventAction == RegistrationEvent && e.EventDate is not null)?.EventDate;

        // A registration date slightly in the future (clock skew) is clamped to age 0.
        return registered is { } date
            ? Math.Max(0, (int)(clock.GetUtcNow() - date).TotalDays)
            : null;
    }

    private sealed record RdapResponse(IReadOnlyList<RdapEvent>? Events);

    private sealed record RdapEvent(string? EventAction, DateTimeOffset? EventDate);
}
