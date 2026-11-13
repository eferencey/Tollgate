// Whether the four sources are the real services or the canned demo ones (no API keys).
// Reported in every response so a demo result can never be mistaken for a real one.
namespace Tollgate.Application;

public enum ScanMode
{
    Live,
    Demo,
}

/// <summary>Registered once at startup so any component can ask which mode the app runs in.</summary>
public sealed record ScanEnvironment(ScanMode Mode);
