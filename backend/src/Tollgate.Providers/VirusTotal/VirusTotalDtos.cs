// The slice of the VirusTotal v3 JSON we read. Property names are snake_case in the API (see VirusTotalProvider.Json).
namespace Tollgate.Providers.VirusTotal;

internal sealed record VtResponse<TAttributes>(VtData<TAttributes>? Data);

internal sealed record VtData<TAttributes>(string? Id, TAttributes? Attributes);

/// <summary>Attributes of GET /urls/{id}.</summary>
internal sealed record VtUrlAttributes(VtStats? LastAnalysisStats);

/// <summary>Attributes of GET /analyses/{id}; Status is "queued", "in-progress" or "completed".</summary>
internal sealed record VtAnalysisAttributes(string? Status, VtStats? Stats);

internal sealed record VtStats(int Malicious, int Suspicious, int Harmless, int Undetected, int Timeout)
{
    /// <summary>
    /// False for an empty or all-zero stats object, which VirusTotal returns for a URL whose analysis has not
    /// produced a single engine verdict yet. That is "not analysed", not "0 engines flagged it".
    /// </summary>
    public bool HasEngineVerdicts => Malicious + Suspicious + Harmless + Undetected + Timeout > 0;
}
