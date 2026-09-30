using TecAssist.Application.Abstractions;

namespace TecAssist.UnitTests.Fakes;

public sealed class FakeChatClient(IReadOnlyList<string> tokens) : IChatClient
{
    private readonly List<IReadOnlyList<ChatMessage>> _receivedPrompts = [];

    public IReadOnlyList<IReadOnlyList<ChatMessage>> ReceivedPrompts => _receivedPrompts;

    public async IAsyncEnumerable<string> StreamCompletionAsync(
        IReadOnlyList<ChatMessage> messages,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _receivedPrompts.Add(messages);
        foreach (var token in tokens)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            yield return token;
        }
    }

    public static FakeChatClient RespondingWith(string answer) => new([answer]);
}
