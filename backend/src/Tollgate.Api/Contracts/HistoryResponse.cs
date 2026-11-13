// The JSON body of GET /api/history: the newest finished scans, one entry per scan.
using Tollgate.Application;

namespace Tollgate.Api.Contracts;

/// <param name="Mode">History is kept per mode; this says which one the entries belong to.</param>
/// <param name="Items">Newest first.</param>
public sealed record HistoryResponse(ScanMode Mode, IReadOnlyList<ScanHistoryEntry> Items);
