// Startup check of the scoring model's numbers (ValidateOnStart): a typo in appsettings.json
// must stop the app with a clear message, not silently produce wrong scores.
using Microsoft.Extensions.Options;
using Tollgate.Domain.Evidence;
using Tollgate.Scoring;

namespace Tollgate.Api.Configuration;

public sealed class ScoringOptionsValidator : IValidateOptions<ScoringOptions>
{
    private const int MaxPoints = 100;

    public ValidateOptionsResult Validate(string? name, ScoringOptions options)
    {
        var errors = new List<string>();

        // The null checks matter: a missing section leaves these "required" properties null at runtime.
        if (options.VirusTotal is null || options.SafeBrowsing is null || options.UrlHaus is null ||
            options.DomainAge is null || options.LocalAnalysis is null || options.VerdictThresholds is null || options.ConfidenceThresholds is null)
        {
            return ValidateOptionsResult.Fail("Scoring section is incomplete: VirusTotal, SafeBrowsing, UrlHaus, DomainAge, LocalAnalysis, VerdictThresholds and ConfidenceThresholds are all required");
        }

        var vt = options.VirusTotal;
        InRange(errors, "VirusTotal:MaliciousPenaltyPerEngine", vt.MaliciousPenaltyPerEngine, 0, MaxPoints);
        InRange(errors, "VirusTotal:MaliciousMaxPenalty", vt.MaliciousMaxPenalty, 0, MaxPoints);
        InRange(errors, "VirusTotal:SuspiciousPenaltyPerEngine", vt.SuspiciousPenaltyPerEngine, 0, MaxPoints);
        InRange(errors, "VirusTotal:SuspiciousMaxPenalty", vt.SuspiciousMaxPenalty, 0, MaxPoints);

        if (vt.MaliciousCaps is null)
        {
            errors.Add("VirusTotal:MaliciousCaps is required (it may be an empty list)");
        }
        else
        {
            foreach (var cap in vt.MaliciousCaps)
            {
                InRange(errors, "VirusTotal:MaliciousCaps:MinEngines", cap.MinEngines, 1, int.MaxValue);
                InRange(errors, "VirusTotal:MaliciousCaps:Cap", cap.Cap, 0, MaxPoints);
            }
        }

        InRange(errors, "SafeBrowsing:ListedPenalty", options.SafeBrowsing.ListedPenalty, 0, MaxPoints);
        InRange(errors, "SafeBrowsing:ListedCap", options.SafeBrowsing.ListedCap, 0, MaxPoints);

        var uh = options.UrlHaus;
        InRange(errors, "UrlHaus:ActiveUrlPenalty", uh.ActiveUrlPenalty, 0, MaxPoints);
        InRange(errors, "UrlHaus:ActiveUrlCap", uh.ActiveUrlCap, 0, MaxPoints);
        InRange(errors, "UrlHaus:PastUrlPenalty", uh.PastUrlPenalty, 0, MaxPoints);
        InRange(errors, "UrlHaus:PastUrlCap", uh.PastUrlCap, 0, MaxPoints);
        InRange(errors, "UrlHaus:HostRecordBasePenalty", uh.HostRecordBasePenalty, 0, MaxPoints);
        InRange(errors, "UrlHaus:HostRecordPenaltyPerRecord", uh.HostRecordPenaltyPerRecord, 0, MaxPoints);
        InRange(errors, "UrlHaus:HostRecordMaxPenalty", uh.HostRecordMaxPenalty, 0, MaxPoints);

        var age = options.DomainAge;
        InRange(errors, "DomainAge:VeryNewBelowDays", age.VeryNewBelowDays, 1, int.MaxValue);
        InRange(errors, "DomainAge:VeryNewPenalty", age.VeryNewPenalty, 0, MaxPoints);
        InRange(errors, "DomainAge:NewBelowDays", age.NewBelowDays, 1, int.MaxValue);
        InRange(errors, "DomainAge:NewPenalty", age.NewPenalty, 0, MaxPoints);
        if (age.NewBelowDays <= age.VeryNewBelowDays)
        {
            errors.Add("DomainAge:NewBelowDays must be greater than VeryNewBelowDays");
        }

        LocalAnalysisValidation.Validate(errors, options.LocalAnalysis);

        var verdict = options.VerdictThresholds;
        InRange(errors, "VerdictThresholds:DangerousBelow", verdict.DangerousBelow, 1, MaxPoints);
        InRange(errors, "VerdictThresholds:SuspiciousBelow", verdict.SuspiciousBelow, 1, MaxPoints);
        if (verdict.SuspiciousBelow <= verdict.DangerousBelow)
        {
            errors.Add("VerdictThresholds:SuspiciousBelow must be greater than DangerousBelow");
        }

        var confidence = options.ConfidenceThresholds;
        var sourceCount = ScanEvidence.QueriedSources.Count;
        InRange(errors, "ConfidenceThresholds:MediumMinSources", confidence.MediumMinSources, 1, sourceCount);
        InRange(errors, "ConfidenceThresholds:HighMinSources", confidence.HighMinSources, 1, sourceCount);
        if (confidence.HighMinSources <= confidence.MediumMinSources)
        {
            errors.Add("ConfidenceThresholds:HighMinSources must be greater than MediumMinSources");
        }

        return errors.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(errors.Select(error => $"Scoring:{error}"));
    }

    internal static void InRange(List<string> errors, string path, int value, int min, int max)
    {
        if (value < min || value > max)
        {
            errors.Add($"{path} must be between {min} and {max}, but is {value}");
        }
    }
}
