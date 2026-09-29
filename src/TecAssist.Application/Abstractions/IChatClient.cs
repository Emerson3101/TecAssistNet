namespace TecAssist.Application.Abstractions;

public interface IChatClient
{
    IAsyncEnumerable<string> StreamCompletionAsync(IReadOnlyList<ChatMessage> messages, CancellationToken cancellationToken = default);
}
