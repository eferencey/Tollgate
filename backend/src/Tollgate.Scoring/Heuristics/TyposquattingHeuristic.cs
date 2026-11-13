// Typosquatting check: does a name in the host imitate a well-known brand ("paypa1", "g00gle", "paypall")?
// Names are compared after folding look-alike characters back to Latin letters, so the same check also
// catches homoglyph imitation. Only the closest match is reported, so one domain costs one finding.
// Deliberately not covered: the exact brand name under another domain ("paypal.evil.com").
using Tollgate.Domain;

namespace Tollgate.Scoring.Heuristics;

public sealed class TyposquattingHeuristic(TyposquattingScoring options) : IUrlHeuristic
{
    private readonly IReadOnlyList<Brand> _brands = options.ProtectedDomains
        .Select(domain => new Brand(domain, domain.Split('.')[0]))
        .ToList();

    public IEnumerable<Finding> Evaluate(Uri url)
    {
        var labels = HostLabels.Of(url);

        // The last label is the top-level domain, which says nothing about the name being imitated.
        // A host that really belongs to a brand ("login.paypal.com") is never an imitation of it.
        if (labels.Count < 2 || _brands.Any(brand => BelongsTo(url.IdnHost, brand.Domain)))
        {
            yield break;
        }

        var closest = labels
            .Take(labels.Count - 1)
            .SelectMany(label => _brands.Select(brand => Compare(label, brand)))
            .OfType<Imitation>()
            .MinBy(match => (match.Kind, match.Distance));

        if (closest is null)
        {
            yield break;
        }

        yield return closest.Kind == ImitationKind.Substitution
            ? new Finding(
                ThreatSource.LocalAnalysis,
                FindingKind.BrandLookalikeCharacters,
                $"Domain name \"{closest.Label}\" imitates {closest.Brand.Domain} using look-alike characters",
                options.SubstitutionPenalty)
            : new Finding(
                ThreatSource.LocalAnalysis,
                FindingKind.BrandTypo,
                $"Domain name \"{closest.Label}\" is a typo away from {closest.Brand.Domain}",
                options.NearMatchPenalty);
    }

    private Imitation? Compare(string label, Brand brand)
    {
        // The brand's own name under another suffix (paypal.de) is common and legitimate.
        if (label == brand.Label)
        {
            return null;
        }

        var folded = LookalikeAlphabet.Fold(label);
        if (folded == brand.Label)
        {
            return new Imitation(label, brand, ImitationKind.Substitution, Distance: 0);
        }

        if (Math.Min(label.Length, brand.Label.Length) < options.NearMatchMinLength)
        {
            return null;
        }

        var distance = EditDistance.Between(folded, brand.Label);
        return distance <= options.MaxEditDistance ? new Imitation(label, brand, ImitationKind.NearMatch, distance) : null;
    }

    private static bool BelongsTo(string host, string brandDomain) =>
        host == brandDomain || host.EndsWith("." + brandDomain, StringComparison.Ordinal);

    private sealed record Brand(string Domain, string Label);

    // Declared in severity order: MinBy picks the first, i.e. the most severe, kind.
    private enum ImitationKind
    {
        Substitution,
        NearMatch,
    }

    private sealed record Imitation(string Label, Brand Brand, ImitationKind Kind, int Distance);
}
