using Microsoft.Extensions.Options;
using TecAssist.Application.Abstractions;
using TecAssist.Application.Options;
using TecAssist.Application.Rag;

namespace TecAssist.UnitTests.Rag;

public sealed class TokenAwareChunkerTests
{
    private static TokenAwareChunker CreateChunker(int chunkSize = 500, int overlap = 50) =>
        new(Options.Create(new RagOptions { ChunkSizeTokens = chunkSize, OverlapTokens = overlap }));

    [Fact]
    public void Chunk_ReturnsEmptyList_ForEmptyText()
    {
        var chunks = CreateChunker().Chunk("   \n\n  ");
        Assert.Empty(chunks);
    }

    [Fact]
    public void Chunk_PacksSmallParagraphs_IntoSingleChunk()
    {
        var text = "First paragraph with a few words.\n\nSecond paragraph with more words.";
        var chunks = CreateChunker(chunkSize: 500).Chunk(text);

        var chunk = Assert.Single(chunks);
        Assert.Contains("First paragraph", chunk.Content);
        Assert.Contains("Second paragraph", chunk.Content);
        Assert.True(chunk.TokenCount > 0);
    }

    [Fact]
    public void Chunk_SplitsOversizedParagraph_WithOverlap()
    {
        var words = Enumerable.Range(0, 50).Select(index => $"word{index}").ToArray();
        var oversized = string.Join(' ', words);
        var chunks = CreateChunker(chunkSize: 10, overlap: 2).Chunk(oversized);

        Assert.True(chunks.Count >= 5);
        Assert.All(chunks, chunk => Assert.True(chunk.TokenCount <= 10));

        var first = chunks[0];
        Assert.Equal(10, first.TokenCount);
        Assert.StartsWith("word0", first.Content);

        Assert.Equal(10, chunks[1].TokenCount);
        Assert.StartsWith("word8", chunks[1].Content);
    }

    [Fact]
    public void Chunk_CreatesNewChunk_WhenBufferWouldOverflow()
    {
        var text = string.Join(
            "\n\n",
            Enumerable.Range(0, 5).Select(index => $"Paragraph {index} alpha beta gamma delta"));

        var chunks = CreateChunker(chunkSize: 8, overlap: 0).Chunk(text);

        Assert.True(chunks.Count >= 2);
        Assert.Contains(chunks, chunk => chunk.Content.Contains("Paragraph 0"));
        Assert.Contains(chunks, chunk => chunk.Content.Contains("Paragraph 4"));
    }

    [Fact]
    public void Chunk_ProducesNoEmptyChunks_ForTextWithBlankLines()
    {
        var text = "alpha beta\n\n\n\ngamma delta\n\n\nepsilon";
        var chunks = CreateChunker().Chunk(text);
        Assert.All(chunks, chunk => Assert.False(string.IsNullOrWhiteSpace(chunk.Content)));
    }
}
