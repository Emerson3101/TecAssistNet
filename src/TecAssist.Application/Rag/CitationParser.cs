using System.Text.RegularExpressions;

namespace TecAssist.Application.Rag;

public static partial class CitationParser
{
    [GeneratedRegex(@"\[(\d{1,3})\]", RegexOptions.Compiled)]
    private static partial Regex CitationPattern();

    public static IReadOnlyList<int> ExtractCitedIndices(string answer, int contextCount)
    {
        var indices = new SortedSet<int>();
        foreach (Match match in CitationPattern().Matches(answer))
        {
            if (int.TryParse(match.Groups[1].Value, out var index) && index >= 1 && index <= contextCount)
            {
                indices.Add(index);
            }
        }

        return [.. indices];
    }
}
