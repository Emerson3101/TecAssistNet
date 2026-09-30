using TecAssist.Application.Abstractions;

namespace TecAssist.UnitTests.Fakes;

public sealed class FakeChunkSearcher(IReadOnlyList<ChunkSearchResult> results) : IChunkSearcher
{
    public int CallCount { get; private set; }

    public Task<IReadOnlyList<ChunkSearchResult>> SearchAsync(
        Guid userId,
        string query,
        int topK,
        CancellationToken cancellationToken = default)
    {
        CallCount++;
        return Task.FromResult(results);
    }

    public static IReadOnlyList<ChunkSearchResult> SingleResult() =>
    [
        new ChunkSearchResult(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Test Document",
            "The maintenance interval is 90 days.",
            0.75),
    ];
}
