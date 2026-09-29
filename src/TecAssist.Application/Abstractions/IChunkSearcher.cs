namespace TecAssist.Application.Abstractions;

public sealed record ChunkSearchResult(
    Guid ChunkId,
    Guid DocumentId,
    string DocumentTitle,
    string Content,
    double Score);

public interface IChunkSearcher
{
    Task<IReadOnlyList<ChunkSearchResult>> SearchAsync(Guid userId, string query, int topK, CancellationToken cancellationToken = default);
}
