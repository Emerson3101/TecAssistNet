namespace TecAssist.Application.Abstractions;

public enum EmbeddingPurpose
{
    Query,

    Passage
}

public interface IEmbeddingClient
{
    Task<IReadOnlyList<float[]>> EmbedAsync(IReadOnlyList<string> texts, EmbeddingPurpose purpose, CancellationToken cancellationToken = default);
}
