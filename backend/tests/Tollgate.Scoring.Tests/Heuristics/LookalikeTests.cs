// The character-level helpers (edit distance, alphabets, folding) and the punycode / homoglyph check built on them.
// Penalties are read from the shipped configuration, so these tests follow the numbers in appsettings.json.
// Look-alike letters are written as code points: U+0430 is Cyrillic "a", U+03BF is Greek "o".
using Tollgate.Domain;
using Tollgate.Scoring.Heuristics;

namespace Tollgate.Scoring.Tests.Heuristics;

public class LookalikeTests
{
    private readonly LookalikeScoring _options = ScoringTestConfig.Options.LocalAnalysis.Lookalike;
    private readonly LookalikeAlphabetHeuristic _heuristic;

    public LookalikeTests() => _heuristic = new LookalikeAlphabetHeuristic(_options);

    private List<Finding> Check(string url) => _heuristic.Evaluate(new Uri(url)).ToList();

    [Theory]
    [InlineData("paypal", "paypal", 0)]
    [InlineData("paypa1", "paypal", 1)]
    [InlineData("paypall", "paypal", 1)]
    [InlineData("gogle", "google", 1)]
    [InlineData("paypla", "paypal", 1)] // two neighbouring letters swapped counts as a single slip
    [InlineData("kitten", "sitting", 3)]
    [InlineData("abc", "", 3)]
    public void Edit_distance_counts_single_character_edits(string from, string to, int expected)
    {
        Assert.Equal(expected, EditDistance.Between(from, to));
    }

    [Theory]
    [InlineData("paypa1", "paypal")]
    [InlineData("g00gle", "google")]
    [InlineData("\U00000430pple", "apple")] // Cyrillic a
    [InlineData("g\U000003BFogle", "google")] // Greek o
    [InlineData("arnazon", "amazon")] // r + n reads as m
    [InlineData("vvhatsapp", "whatsapp")]
    [InlineData("example", "example")]
    public void Folding_turns_look_alike_characters_back_into_the_letters_they_imitate(string label, string expected)
    {
        Assert.Equal(expected, LookalikeAlphabet.Fold(label));
    }

    [Fact]
    public void Letters_are_assigned_to_their_alphabet_and_digits_to_none()
    {
        Assert.Equal(Alphabet.Latin, LookalikeAlphabet.AlphabetOf('a'));
        Assert.Equal(Alphabet.Latin, LookalikeAlphabet.AlphabetOf('\U0000011F')); // Turkish g with breve
        Assert.Equal(Alphabet.Cyrillic, LookalikeAlphabet.AlphabetOf('\U00000430'));
        Assert.Equal(Alphabet.Greek, LookalikeAlphabet.AlphabetOf('\U000003BF'));
        Assert.Equal(Alphabet.Other, LookalikeAlphabet.AlphabetOf('\U000065E5')); // a CJK character
        Assert.Null(LookalikeAlphabet.AlphabetOf('7'));
        Assert.Null(LookalikeAlphabet.AlphabetOf('-'));
    }

    [Theory]
    [InlineData("https://example.com/")]
    [InlineData("https://t\U000000FCrkiye.gov.tr/")] // accents are still Latin: ordinary European names are not flagged
    [InlineData("https://\U000000E7i\U000000E7ek.com/")]
    [InlineData("http://192.168.0.1/")] // an address has no name to inspect
    [InlineData("https://localhost/")]
    public void Ordinary_hosts_have_no_finding(string url)
    {
        Assert.Empty(Check(url));
    }

    [Theory]
    [InlineData("https://\U00000430pple.com/", "Cyrillic")] // Latin with one Cyrillic letter
    [InlineData("https://g\U000003BFogle.com/", "Greek")]
    [InlineData("https://xn--pple-43d.com/", "Cyrillic")] // the same name as it travels on the wire (punycode)
    public void Mixing_alphabets_in_one_word_is_penalised_heavily(string url, string alphabet)
    {
        var finding = Assert.Single(Check(url));

        Assert.Equal(ThreatSource.LocalAnalysis, finding.Source);
        Assert.Equal(_options.MixedAlphabetPenalty, finding.PointsDeducted);
        Assert.Contains(alphabet, finding.Description);
        Assert.Null(finding.ScoreCap);
    }

    [Fact]
    public void A_domain_wholly_in_another_alphabet_costs_the_smaller_penalty()
    {
        // Five Cyrillic letters (U+043C U+043E U+0441 U+043A U+0432), no Latin letter in the word.
        var finding = Assert.Single(Check("https://\U0000043C\U0000043E\U00000441\U0000043A\U00000432.com/"));

        Assert.Equal(_options.NonLatinAlphabetPenalty, finding.PointsDeducted);
        Assert.True(_options.NonLatinAlphabetPenalty < _options.MixedAlphabetPenalty);
    }

    [Fact]
    public void A_mixed_word_is_reported_once_not_also_as_a_non_latin_domain()
    {
        // A mixed label (Cyrillic a + "pple") under a Cyrillic top-level domain (U+0440 U+0444).
        Assert.Single(Check("https://\U00000430pple.\U00000440\U00000444/"));
    }
}
