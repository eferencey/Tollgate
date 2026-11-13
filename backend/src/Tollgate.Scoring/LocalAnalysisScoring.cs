// Tunable numbers of the local (URL-shape) analysis, the "LocalAnalysis" part of the Scoring section.
// Like the rest of ScoringOptions there are deliberately no defaults: appsettings.json is the single source of truth.
namespace Tollgate.Scoring;

public sealed record LocalAnalysisScoring
{
    public required LookalikeScoring Lookalike { get; init; }
    public required TyposquattingScoring Typosquatting { get; init; }
    public required SuspiciousTldScoring SuspiciousTld { get; init; }
    public required PenaltyScoring IpHost { get; init; }
    public required PenaltyScoring UserInfo { get; init; }
}

/// <summary>Punycode / homoglyph check: names written in a non-Latin alphabet, and names that mix alphabets.</summary>
public sealed record LookalikeScoring
{
    /// <summary>Domain written in Cyrillic, Greek or another non-Latin alphabet; legitimate often, but it can hide Latin look-alikes.</summary>
    public required int NonLatinAlphabetPenalty { get; init; }

    /// <summary>One word mixing Latin with Cyrillic or Greek letters; almost only seen in impersonation.</summary>
    public required int MixedAlphabetPenalty { get; init; }
}

public sealed record TyposquattingScoring
{
    /// <summary>Well-known domains worth imitating; the first label of each ("paypal" of "paypal.com") is what gets compared.</summary>
    public required IReadOnlyList<string> ProtectedDomains { get; init; }

    /// <summary>Names shorter than this are never called a near-miss: short names are close to too many real words.</summary>
    public required int NearMatchMinLength { get; init; }

    public required int MaxEditDistance { get; init; }

    /// <summary>The name becomes the brand once look-alike characters are swapped back ("paypa1", "g00gle").</summary>
    public required int SubstitutionPenalty { get; init; }

    /// <summary>The name is within <see cref="MaxEditDistance"/> edits of the brand ("paypall").</summary>
    public required int NearMatchPenalty { get; init; }
}

public sealed record SuspiciousTldScoring
{
    /// <summary>Top-level domains abused far more than they are used, written without the dot.</summary>
    public required IReadOnlyList<string> Tlds { get; init; }

    public required int Penalty { get; init; }
}

/// <summary>A check with one fixed price.</summary>
public sealed record PenaltyScoring
{
    public required int Penalty { get; init; }
}
