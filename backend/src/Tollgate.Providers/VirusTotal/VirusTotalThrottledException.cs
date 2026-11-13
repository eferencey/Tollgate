// Signals that VirusTotal was not called on purpose (quota or queue limit), as opposed to a network failure.
namespace Tollgate.Providers.VirusTotal;

public sealed class VirusTotalThrottledException(string message) : Exception(message);
