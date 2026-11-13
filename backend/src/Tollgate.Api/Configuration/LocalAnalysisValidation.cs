// Startup check of the local-analysis numbers and lists (called by ScoringOptionsValidator). The lists need
// care: a brand without a dot or a TLD written ".top" would silently never match anything.
using Tollgate.Scoring;

namespace Tollgate.Api.Configuration;

internal static class LocalAnalysisValidation
{
    private const int MaxPoints = 100;
    private const int MaxEditDistance = 3;

    public static void Validate(List<string> errors, LocalAnalysisScoring local)
    {
        if (local.Lookalike is null || local.Typosquatting is null || local.SuspiciousTld is null || local.IpHost is null || local.UserInfo is null)
        {
            errors.Add("LocalAnalysis is incomplete: Lookalike, Typosquatting, SuspiciousTld, IpHost and UserInfo are all required");
            return;
        }

        ScoringOptionsValidator.InRange(errors, "LocalAnalysis:Lookalike:NonLatinAlphabetPenalty", local.Lookalike.NonLatinAlphabetPenalty, 0, MaxPoints);
        ScoringOptionsValidator.InRange(errors, "LocalAnalysis:Lookalike:MixedAlphabetPenalty", local.Lookalike.MixedAlphabetPenalty, 0, MaxPoints);

        var typo = local.Typosquatting;
        ScoringOptionsValidator.InRange(errors, "LocalAnalysis:Typosquatting:NearMatchMinLength", typo.NearMatchMinLength, 1, int.MaxValue);
        ScoringOptionsValidator.InRange(errors, "LocalAnalysis:Typosquatting:MaxEditDistance", typo.MaxEditDistance, 1, MaxEditDistance);
        ScoringOptionsValidator.InRange(errors, "LocalAnalysis:Typosquatting:SubstitutionPenalty", typo.SubstitutionPenalty, 0, MaxPoints);
        ScoringOptionsValidator.InRange(errors, "LocalAnalysis:Typosquatting:NearMatchPenalty", typo.NearMatchPenalty, 0, MaxPoints);
        if (typo.ProtectedDomains is null)
        {
            errors.Add("LocalAnalysis:Typosquatting:ProtectedDomains is required (it may be an empty list)");
        }
        else
        {
            errors.AddRange(typo.ProtectedDomains
                .Where(domain => !IsLowerCaseDomain(domain))
                .Select(domain => $"LocalAnalysis:Typosquatting:ProtectedDomains entry \"{domain}\" must be a lower-case domain such as \"paypal.com\""));
        }

        ScoringOptionsValidator.InRange(errors, "LocalAnalysis:SuspiciousTld:Penalty", local.SuspiciousTld.Penalty, 0, MaxPoints);
        if (local.SuspiciousTld.Tlds is null)
        {
            errors.Add("LocalAnalysis:SuspiciousTld:Tlds is required (it may be an empty list)");
        }
        else
        {
            errors.AddRange(local.SuspiciousTld.Tlds
                .Where(tld => string.IsNullOrWhiteSpace(tld) || tld.Contains('.'))
                .Select(tld => $"LocalAnalysis:SuspiciousTld:Tlds entry \"{tld}\" must be a bare TLD such as \"top\" (no dot)"));
        }

        ScoringOptionsValidator.InRange(errors, "LocalAnalysis:IpHost:Penalty", local.IpHost.Penalty, 0, MaxPoints);
        ScoringOptionsValidator.InRange(errors, "LocalAnalysis:UserInfo:Penalty", local.UserInfo.Penalty, 0, MaxPoints);
    }

    private static bool IsLowerCaseDomain(string? domain) =>
        !string.IsNullOrWhiteSpace(domain) && domain.Contains('.') && domain == domain.ToLowerInvariant() && !domain.Contains(' ');
}
