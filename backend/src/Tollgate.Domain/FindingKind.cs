// The machine-readable type of a finding. The description is prose for people and may be reworded at any time;
// this code is the stable identity that clients (the UI's plain-language explanations) key on.
namespace Tollgate.Domain;

public enum FindingKind
{
    // VirusTotal
    EnginesMalicious,
    EnginesSuspicious,

    // Google Safe Browsing
    SafeBrowsingListed,

    // URLhaus
    UrlActivelyDistributingMalware,
    UrlDistributedMalwareInPast,
    DomainHasMalwareRecords,

    // RDAP
    DomainRecentlyRegistered,

    // Local analysis (the shape of the URL itself)
    MixedAlphabetDomain,
    NonLatinAlphabetDomain,
    BrandLookalikeCharacters,
    BrandTypo,
    SuspiciousTld,
    RawIpAddressHost,
    HiddenBehindUserInfo,
}
