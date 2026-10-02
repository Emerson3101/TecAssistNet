using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TecAssist.Application.Abstractions;
using TecAssist.Application.Options;
using TecAssist.Infrastructure.Nlp;

namespace TecAssist.UnitTests.Nlp;

public sealed class ResilientChatClientTests
{
    private sealed class ScriptedChatClient(params IReadOnlyList<string>[] attempts) : IChatClient
    {
        private readonly Queue<IReadOnlyList<string>> _attempts = new(attempts);

        public int Calls { get; private set; }

        public async IAsyncEnumerable<string> StreamCompletionAsync(
            IReadOnlyList<ChatMessage> messages,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            Calls++;
            foreach (var token in _attempts.Dequeue())
            {
                await Task.Yield();
                cancellationToken.ThrowIfCancellationRequested();
                yield return token;
            }
        }
    }

    private sealed class ThrowingChatClient : IChatClient
    {
        public int Calls { get; private set; }

        public async IAsyncEnumerable<string> StreamCompletionAsync(
            IReadOnlyList<ChatMessage> messages,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            Calls++;
            yield return string.Empty;
            await Task.Yield();
            throw new HttpRequestException("provider is down");
        }
    }

    private static ResilientChatClient Create(IChatClient inner, Action<RagOptions>? configure = null)
    {
        var ragOptions = new RagOptions { ChatStreamRetryDelay = TimeSpan.Zero };
        configure?.Invoke(ragOptions);
        return new ResilientChatClient(
            inner,
            NullLogger<ResilientChatClient>.Instance,
            Options.Create(ragOptions));
    }

    private static async Task<List<string>> CollectAsync(ResilientChatClient client)
    {
        var tokens = new List<string>();
        await foreach (var token in client.StreamCompletionAsync([]))
        {
            tokens.Add(token);
        }
        return tokens;
    }

    [Fact]
    public async Task RetriesOnce_WhenFirstStreamIsEmpty()
    {
        var scripted = new ScriptedChatClient([], ["recovered", " answer"]);
        var client = Create(scripted);

        var tokens = await CollectAsync(client);

        Assert.Equal(["recovered", " answer"], tokens);
        Assert.Equal(2, scripted.Calls);
    }

    [Fact]
    public async Task YieldsNothing_WhenEveryStreamIsEmpty()
    {
        var scripted = new ScriptedChatClient([], []);
        var client = Create(scripted);

        var tokens = await CollectAsync(client);

        Assert.Empty(tokens);
        Assert.Equal(2, scripted.Calls);
    }

    [Fact]
    public async Task DoesNotRetry_WhenStreamThrows()
    {
        var throwing = new ThrowingChatClient();
        var client = Create(throwing);

        await Assert.ThrowsAsync<HttpRequestException>(() => CollectAsync(client));

        Assert.Equal(1, throwing.Calls);
    }

    [Fact]
    public async Task DoesNotRetry_WhenContentWasStreamed()
    {
        var scripted = new ScriptedChatClient(["content"]);
        var client = Create(scripted);

        var tokens = await CollectAsync(client);

        Assert.Equal(["content"], tokens);
        Assert.Equal(1, scripted.Calls);
    }

    [Fact]
    public async Task DoesNotRetry_WhenRetriesAreDisabled()
    {
        var scripted = new ScriptedChatClient(Array.Empty<string>());
        var client = Create(scripted, options => options.ChatStreamEmptyRetries = 0);

        var tokens = await CollectAsync(client);

        Assert.Empty(tokens);
        Assert.Equal(1, scripted.Calls);
    }
}
