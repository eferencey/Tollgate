// The two tables as plain classes. Rows are storage shapes only; the rest of the app never sees them.
using Tollgate.Domain;

namespace Tollgate.Infrastructure.Persistence;

/// <summary>One cache entry per URL; the evidence is a JSON document because it is always read and written as a whole.</summary>
public sealed class CachedEvidenceRow
{
    public required string Url { get; set; }

    public required string EvidenceJson { get; set; }

    public required DateTimeOffset ScannedAt { get; set; }
}

/// <summary>One finished scan. Rows are added or deleted (retention, user's request) but never changed.</summary>
public sealed class ScanHistoryRow
{
    public long Id { get; set; }

    public required string Url { get; set; }

    public required string Host { get; set; }

    public required int Score { get; set; }

    public required Verdict Verdict { get; set; }

    public required Confidence Confidence { get; set; }

    public required DateTimeOffset ScannedAt { get; set; }
}
