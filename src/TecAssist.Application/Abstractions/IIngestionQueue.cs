namespace TecAssist.Application.Abstractions;

public sealed record IngestionJob(Guid DocumentId, string Text);

public interface IIngestionQueue
{
    bool TryEnqueue(IngestionJob job);

    IAsyncEnumerable<IngestionJob> DequeueAllAsync(CancellationToken cancellationToken);
}
