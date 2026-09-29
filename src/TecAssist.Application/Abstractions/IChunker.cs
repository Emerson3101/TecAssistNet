namespace TecAssist.Application.Abstractions;

public sealed record TextChunk(string Content, int TokenCount);

public interface IChunker
{
    IReadOnlyList<TextChunk> Chunk(string text);
}
