using TecAssist.Application.Rag;

namespace TecAssist.UnitTests.Rag;

public sealed class CitationParserTests
{
    [Fact]
    public void ExtractCitedIndices_ReturnsUniqueValidIndices()
    {
        var indices = CitationParser.ExtractCitedIndices("Answer one [1] and again [1], also [3].", contextCount: 5);

        Assert.Equal([1, 3], indices);
    }

    [Fact]
    public void ExtractCitedIndices_IgnoresOutOfRangeIndices()
    {
        var indices = CitationParser.ExtractCitedIndices("Bad [0], bad [9], good [2].", contextCount: 3);

        Assert.Equal([2], indices);
    }

    [Fact]
    public void ExtractCitedIndices_ReturnsEmpty_WhenNoCitations()
    {
        var indices = CitationParser.ExtractCitedIndices("No citations here.", contextCount: 3);

        Assert.Empty(indices);
    }

    [Fact]
    public void ExtractCitedIndices_HandlesLargeNumbers_AndNonGreedyMatches()
    {
        var indices = CitationParser.ExtractCitedIndices("[12] and [999] and [abc]", contextCount: 12);

        Assert.Equal([12], indices);
    }
}
