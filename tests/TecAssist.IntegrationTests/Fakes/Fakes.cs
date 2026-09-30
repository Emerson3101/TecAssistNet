using System.Net.Http.Json;
using System.Text.Json;
using TecAssist.Application.Abstractions;

namespace TecAssist.IntegrationTests.Fakes;

public sealed class FakeEmbeddingClient(int dimensions) : IEmbeddingClient
{
    public Task<IReadOnlyList<float[]>> EmbedAsync(
        IReadOnlyList<string> texts,
        EmbeddingPurpose purpose,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<float[]> vectors = Enumerable
            .Range(0, texts.Count)
            .Select(_ => Enumerable.Repeat(0.5f, dimensions).ToArray())
            .ToArray();
        return Task.FromResult(vectors);
    }
}

public sealed class FakeStreamingChatClient(params string[] tokens) : IChatClient
{
    public const string ExpectedAnswer = "The maintenance interval is 90 days [1].";

    public async IAsyncEnumerable<string> StreamCompletionAsync(
        IReadOnlyList<ChatMessage> messages,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var token in tokens)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            yield return token;
        }
    }
}

public sealed record SseFrame(string Event, JsonElement Data)
{
    public T Deserialize<T>(JsonSerializerOptions options) => Data.Deserialize<T>(options)!;

    public static List<SseFrame> Parse(string rawBody)
    {
        var frames = new List<SseFrame>();
        foreach (var block in rawBody.Split("\n\n", StringSplitOptions.RemoveEmptyEntries))
        {
            string? eventName = null;
            string? dataLine = null;
            foreach (var line in block.Split('\n'))
            {
                if (line.StartsWith("event: "))
                {
                    eventName = line["event: ".Length..].Trim();
                }
                else if (line.StartsWith("data: "))
                {
                    dataLine = line["data: ".Length..];
                }
            }

            if (eventName is not null && dataLine is not null)
            {
                frames.Add(new SseFrame(eventName, JsonDocument.Parse(dataLine).RootElement));
            }
        }

        return frames;
    }
}
