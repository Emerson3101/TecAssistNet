using System.Threading.Channels;
using Microsoft.Extensions.Options;
using TecAssist.Application.Abstractions;
using TecAssist.Application.Options;

namespace TecAssist.Application.Queues;

public sealed class ChannelIngestionQueue(IOptions<RagOptions> options) : IIngestionQueue
{
    private readonly Channel<IngestionJob> _channel = Channel.CreateBounded<IngestionJob>(
        Math.Max(1, options.Value.IngestionQueueCapacity));

    public bool TryEnqueue(IngestionJob job) => _channel.Writer.TryWrite(job);

    public IAsyncEnumerable<IngestionJob> DequeueAllAsync(CancellationToken cancellationToken) =>
        _channel.Reader.ReadAllAsync(cancellationToken);
}
