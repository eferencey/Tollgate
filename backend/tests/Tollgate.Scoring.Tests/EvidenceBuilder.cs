// Test helper: starts from "every source clean" and lets a test change only what it cares about.
using Tollgate.Domain.Evidence;

namespace Tollgate.Scoring.Tests;

internal static class EvidenceBuilder
{
    private const int EstablishedDomainAgeDays = 3650;

    public static ScanEvidence Clean() => new(
        new VirusTotalEvidence(MaliciousEngines: 0, SuspiciousEngines: 0),
        new SafeBrowsingEvidence(IsListed: false),
        new UrlHausEvidence(UrlHausUrlStatus.NotListed, HostRecordCount: 0),
        new RdapEvidence(EstablishedDomainAgeDays));

    public static ScanEvidence WithVirusTotal(int malicious = 0, int suspicious = 0) =>
        Clean() with { VirusTotal = new VirusTotalEvidence(malicious, suspicious) };

    public static ScanEvidence WithSafeBrowsingListed() =>
        Clean() with { SafeBrowsing = new SafeBrowsingEvidence(IsListed: true) };

    public static ScanEvidence WithUrlHaus(UrlHausUrlStatus status = UrlHausUrlStatus.NotListed, int hostRecords = 0) =>
        Clean() with { UrlHaus = new UrlHausEvidence(status, hostRecords) };

    public static ScanEvidence WithDomainAge(int? days) =>
        Clean() with { Rdap = new RdapEvidence(days) };
}
