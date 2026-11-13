// How many single-character edits turn one string into another (insert, delete, replace, or swap two
// neighbours). Swapping neighbours counts as one edit because it is the commonest typing slip ("paypla").
namespace Tollgate.Scoring.Heuristics;

internal static class EditDistance
{
    public static int Between(string from, string to)
    {
        var table = new int[from.Length + 1, to.Length + 1];

        for (var i = 0; i <= from.Length; i++)
        {
            table[i, 0] = i;
        }

        for (var j = 0; j <= to.Length; j++)
        {
            table[0, j] = j;
        }

        for (var i = 1; i <= from.Length; i++)
        {
            for (var j = 1; j <= to.Length; j++)
            {
                var replaceCost = from[i - 1] == to[j - 1] ? 0 : 1;
                table[i, j] = Math.Min(
                    Math.Min(table[i - 1, j] + 1, table[i, j - 1] + 1),
                    table[i - 1, j - 1] + replaceCost);

                var isNeighbourSwap = i > 1 && j > 1 && from[i - 1] == to[j - 2] && from[i - 2] == to[j - 1];
                if (isNeighbourSwap)
                {
                    table[i, j] = Math.Min(table[i, j], table[i - 2, j - 2] + 1);
                }
            }
        }

        return table[from.Length, to.Length];
    }
}
