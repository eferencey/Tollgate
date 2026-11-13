// Unicode facts about look-alike letters: which alphabet a letter belongs to, and how to fold a name
// down to the plain Latin letters it imitates. These are fixed facts about characters, not tunable
// scoring numbers, so they live in code; the penalties that use them live in appsettings.json.
// Look-alike letters are written as code points (\U0000xxxx) on purpose: a reader cannot tell a Cyrillic
// "а" from a Latin "a" on screen, which is exactly the attack this file defends against.
namespace Tollgate.Scoring.Heuristics;

internal enum Alphabet
{
    Latin,
    Cyrillic,
    Greek,
    Other,
}

internal static class LookalikeAlphabet
{
    // Letters from other alphabets (and a few Latin extras) that are drawn almost identically to a Latin letter.
    private static readonly IReadOnlyDictionary<char, char> LookalikeLetters = new Dictionary<char, char>
    {
        // Cyrillic: а е о р с х у і ј ѕ һ ԁ ӏ ԛ ԝ
        ['\U00000430'] = 'a', ['\U00000435'] = 'e', ['\U0000043E'] = 'o', ['\U00000440'] = 'p', ['\U00000441'] = 'c',
        ['\U00000445'] = 'x', ['\U00000443'] = 'y', ['\U00000456'] = 'i', ['\U00000458'] = 'j', ['\U00000455'] = 's',
        ['\U000004BB'] = 'h', ['\U00000501'] = 'd', ['\U000004CF'] = 'l', ['\U0000051B'] = 'q', ['\U0000051D'] = 'w',
        // Greek: ο α ν ρ ι κ υ χ
        ['\U000003BF'] = 'o', ['\U000003B1'] = 'a', ['\U000003BD'] = 'v', ['\U000003C1'] = 'p', ['\U000003B9'] = 'i',
        ['\U000003BA'] = 'k', ['\U000003C5'] = 'u', ['\U000003C7'] = 'x',
        // Latin extras: dotless i, script g
        ['\U00000131'] = 'i', ['\U00000261'] = 'g',
    };

    // Digits people swap in for letters ("paypa1", "g00gle"). "rn" and "vv" are read as "m" and "w" at a glance.
    private static readonly IReadOnlyDictionary<char, char> LookalikeDigits = new Dictionary<char, char>
    {
        ['0'] = 'o', ['1'] = 'l', ['3'] = 'e', ['5'] = 's',
    };

    private static readonly (string Pair, string Letter)[] LookalikePairs = [("rn", "m"), ("vv", "w")];

    /// <summary>The alphabet of a letter, or null for anything that is not a letter (digits, hyphens).</summary>
    public static Alphabet? AlphabetOf(char c)
    {
        if (!char.IsLetter(c))
        {
            return null;
        }

        return c switch
        {
            // Basic Latin, Latin-1, Latin Extended-A/B, and Latin Extended Additional
            <= '\U0000024F' or (>= '\U00001E00' and <= '\U00001EFF') => Alphabet.Latin,
            >= '\U00000400' and <= '\U0000052F' => Alphabet.Cyrillic,
            // Greek and Coptic, and Greek Extended
            (>= '\U00000370' and <= '\U000003FF') or (>= '\U00001F00' and <= '\U00001FFF') => Alphabet.Greek,
            _ => Alphabet.Other,
        };
    }

    /// <summary>The plain Latin "skeleton" a name would be mistaken for at a glance.</summary>
    public static string Fold(string label)
    {
        var folded = new string(label.Select(FoldChar).ToArray());
        foreach (var (pair, letter) in LookalikePairs)
        {
            folded = folded.Replace(pair, letter, StringComparison.Ordinal);
        }

        return folded;
    }

    private static char FoldChar(char c) =>
        LookalikeLetters.TryGetValue(c, out var letter) ? letter
        : LookalikeDigits.TryGetValue(c, out var digitLetter) ? digitLetter
        : c;
}
