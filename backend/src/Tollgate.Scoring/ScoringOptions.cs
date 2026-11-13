// Every tunable number of the scoring model, bound from the "Scoring" section of appsettings.json.
// There are deliberately no defaults here: the configuration file is the single source of truth.
namespace Tollgate.Scoring;

public sealed record ScoringOptions
{
    public const string SectionName = "Scoring";

    public required VirusTotalScoring VirusTotal { get; init; }
    public required SafeBrowsingScoring SafeBrowsing { get; init; }
    public required UrlHausScoring UrlHaus { get; init; }
    public required DomainAgeScoring DomainAge { get; init; }
    public required LocalAnalysisScoring LocalAnalysis { get; init; }
    public required VerdictThresholds VerdictThresholds { get; init; }
    public required ConfidenceThresholds ConfidenceThresholds { get; init; }
}

public sealed record VirusTotalScoring
{
    public required int MaliciousPenaltyPerEngine { get; init; }
    public required int MaliciousMaxPenalty { get; init; }
    public required int SuspiciousPenaltyPerEngine { get; init; }
    public required int SuspiciousMaxPenalty { get; init; }

    /// <summary>Score ceilings that kick in once enough engines say "malicious"; the lowest matching one wins.</summary>
    public required IReadOnlyList<EngineCountCap> MaliciousCaps { get; init; }
}

public sealed record EngineCountCap
{
    public required int MinEngines { get; init; }
    public required int Cap { get; init; }
}

public sealed record SafeBrowsingScoring
{
    public required int ListedPenalty { get; init; }
    public required int ListedCap { get; init; }
}

public sealed record UrlHausScoring
{
    public required int ActiveUrlPenalty { get; init; }
    public required int ActiveUrlCap { get; init; }
    public required int PastUrlPenalty { get; init; }
    public required int PastUrlCap { get; init; }

    /// <summary>Host-record penalty is BasePenalty + PenaltyPerRecord x count, never more than MaxPenalty.</summary>
    public required int HostRecordBasePenalty { get; init; }
    public required int HostRecordPenaltyPerRecord { get; init; }
    public required int HostRecordMaxPenalty { get; init; }
}

public sealed record DomainAgeScoring
{
    public required int VeryNewBelowDays { get; init; }
    public required int VeryNewPenalty { get; init; }
    public required int NewBelowDays { get; init; }
    public required int NewPenalty { get; init; }
}

/// <summary>A score below DangerousBelow is Dangerous, below SuspiciousBelow is Suspicious, otherwise Safe.</summary>
public sealed record VerdictThresholds
{
    public required int DangerousBelow { get; init; }
    public required int SuspiciousBelow { get; init; }
}

/// <summary>Number of responding main sources needed for each confidence level.</summary>
public sealed record ConfidenceThresholds
{
    public required int HighMinSources { get; init; }
    public required int MediumMinSources { get; init; }
}
