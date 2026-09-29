namespace TecAssist.Application.Abstractions;

public interface IChatClient
{
    Task<string> CompleteAsync(IReadOnlyList<ChatMessage> messages, CancellationToken cancellationToken = default);

    IAsyncEnumerable<string> StreamCompletionAsync(IReadOnlyList<ChatMessage> messages, CancellationToken cancellationToken = default);
}
