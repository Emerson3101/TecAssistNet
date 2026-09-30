using TecAssist.Application.Abstractions;

namespace TecAssist.UnitTests.Fakes;

public sealed class FakeEmbeddingClient(int dimensions, Exception? throwOnCall = null) : IEmbeddingClient
{
    public int CallCount { get; private set; }

    public Task<IReadOnlyList<float[]>> EmbedAsync(
        IReadOnlyList<string> texts,
        EmbeddingPurpose purpose,
        CancellationToken cancellationToken = default)
    {
        CallCount++;

        if (throwOnCall is not null)
        {
            return Task.FromException<IReadOnlyList<float[]>>(throwOnCall);
        }

        IReadOnlyList<float[]> vectors = Enumerable
            .Range(0, texts.Count)
            .Select(_ => Enumerable.Repeat(0.5f, dimensions).ToArray())
            .ToArray();
        return Task.FromResult(vectors);
    }
}
