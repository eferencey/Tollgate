// Typosquatting: names that imitate a protected brand, and the legitimate look-alikes that must stay unflagged.
// U+0430 below is Cyrillic "a" (it looks exactly like the Latin one).
using Tollgate.Domain;
using Tollgate.Scoring.Heuristics;

namespace Tollgate.Scoring.Tests.Heuristics;

public class TyposquattingTests
{
    private readonly TyposquattingScoring _options = ScoringTestConfig.Options.LocalAnalysis.Typosquatting;
    private readonly TyposquattingHeuristic _heuristic;

    public TyposquattingTests() => _heuristic = new TyposquattingHeuristic(_options);

    private List<Finding> Check(string url) => _heuristic.Evaluate(new Uri(url)).ToList();

    [Theory]
    [InlineData("https://paypa1.com/", "paypal.com")]
    [InlineData("https://g00gle.com/", "google.com")]
    [InlineData("https://faceb00k.net/", "facebook.com")] // another suffix does not make it innocent
    [InlineData("https://arnazon.com/", "amazon.com")]
    [InlineData("https://\U00000430pple.com/", "apple.com")] // Cyrillic a
    [InlineData("https://xn--pple-43d.com/", "apple.com")] // the same, as it travels on the wire
    [InlineData("https://login.paypa1.com/account", "paypal.com")] // a deeper name is still the same imitation
    public void Look_alike_characters_standing_in_for_a_brand_cost_the_substitution_penalty(string url, string brand)
    {
        var finding = Assert.Single(Check(url));

        Assert.Equal(ThreatSource.LocalAnalysis, finding.Source);
        Assert.Equal(_options.SubstitutionPenalty, finding.PointsDeducted);
        Assert.Contains(brand, finding.Description);
        Assert.Null(finding.ScoreCap);
    }

    [Theory]
    [InlineData("https://paypall.com/", "paypal.com")] // one letter too many
    [InlineData("https://gooogle.com/", "google.com")]
    [InlineData("https://paypla.com/", "paypal.com")] // two letters swapped
    [InlineData("https://amazom.com/", "amazon.com")] // one letter replaced
    [InlineData("https://trendyol-.com/", "trendyol.com")]
    public void A_typo_away_from_a_brand_costs_the_near_match_penalty(string url, string brand)
    {
        var finding = Assert.Single(Check(url));

        Assert.Equal(_options.NearMatchPenalty, finding.PointsDeducted);
        Assert.Contains(brand, finding.Description);
    }

    [Theory]
    [InlineData("https://paypal.com/")]
    [InlineData("https://www.paypal.com/signin")]
    [InlineData("https://login.paypal.com/")]
    [InlineData("https://paypal.de/")] // the real brand name under another suffix is common and legitimate
    [InlineData("https://google.com.tr/")]
    [InlineData("https://turkiye.gov.tr/")]
    [InlineData("https://example.com/")]
    [InlineData("https://eba.com/")] // too short to call a near-miss of "ebay"
    [InlineData("https://bank.com/")] // two edits from "akbank"
    [InlineData("http://192.168.0.1/")]
    [InlineData("https://paypa1/")] // a single label has no domain name to imitate with
    public void Real_brand_hosts_and_unrelated_names_have_no_finding(string url)
    {
        Assert.Empty(Check(url));
    }

    [Fact]
    public void Several_matches_in_one_host_are_reported_as_a_single_finding_with_the_most_severe_kind()
    {
        var finding = Assert.Single(Check("https://paypall.paypa1.com/"));

        Assert.Equal(_options.SubstitutionPenalty, finding.PointsDeducted);
    }

    [Fact]
    public void The_distance_allowed_comes_from_configuration()
    {
        var loose = new TyposquattingHeuristic(_options with { MaxEditDistance = 2 });

        // "goooogle" is two edits from "google": only the looser setting catches it.
        Assert.Empty(_heuristic.Evaluate(new Uri("https://goooogle.com/")));
        Assert.Single(loose.Evaluate(new Uri("https://goooogle.com/")));
    }
}
