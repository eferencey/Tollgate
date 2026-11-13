// Punycode / homoglyph check. A domain in another alphabet is shipped as "xn--..." (punycode), and
// Cyrillic "а" is indistinguishable from Latin "a" on screen. Two tiers, only the worse one is reported:
// a word mixing Latin with Cyrillic/Greek letters (impersonation), or a domain in a non-Latin alphabet.
// Latin letters with accents (ç, ğ, ü) are ordinary European names and are not flagged.
using Tollgate.Domain;

namespace Tollgate.Scoring.Heuristics;

public sealed class LookalikeAlphabetHeuristic(LookalikeScoring options) : IUrlHeuristic
{
    public IEnumerable<Finding> Evaluate(Uri url)
    {
        var labels = HostLabels.Of(url);

        // Latin mixed with Cyrillic or Greek: the only mixes whose letters can be drawn like Latin ones.
        foreach (var label in labels)
        {
            var alphabets = label.Select(LookalikeAlphabet.AlphabetOf).OfType<Alphabet>().ToHashSet();
            if (alphabets.Contains(Alphabet.Latin) && (alphabets.Contains(Alphabet.Cyrillic) || alphabets.Contains(Alphabet.Greek)))
            {
                var other = alphabets.Contains(Alphabet.Cyrillic) ? "Cyrillic" : "Greek";
                yield return new Finding(
                    ThreatSource.LocalAnalysis,
                    FindingKind.MixedAlphabetDomain,
                    $"Domain name \"{label}\" mixes Latin and {other} letters, a trick used to imitate other sites",
                    options.MixedAlphabetPenalty);
                yield break;
            }
        }

        var nonLatin = labels.FirstOrDefault(label => label.Any(c => LookalikeAlphabet.AlphabetOf(c) is Alphabet.Cyrillic or Alphabet.Greek or Alphabet.Other));
        if (nonLatin is not null)
        {
            yield return new Finding(
                ThreatSource.LocalAnalysis,
                FindingKind.NonLatinAlphabetDomain,
                $"Domain name \"{nonLatin}\" is written in a non-Latin alphabet, which can hide letters that look Latin",
                options.NonLatinAlphabetPenalty);
        }
    }
}
