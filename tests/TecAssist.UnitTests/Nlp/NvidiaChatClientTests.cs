using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TecAssist.Infrastructure.Nlp;

namespace TecAssist.UnitTests.Nlp;

public sealed class NvidiaChatClientTests
{
    private sealed class SequenceHandler : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> _responses;

        public SequenceHandler(params HttpResponseMessage[] responses)
        {
            _responses = new Queue<HttpResponseMessage>(responses);
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(_responses.Dequeue());
        }
    }

    private static HttpResponseMessage Sse(params string[] frames) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(string.Join("\n\n", frames)),
        };

    private static NvidiaChatClient CreateClient(HttpMessageHandler handler) =>
        new(
            new HttpClient(handler) { BaseAddress = new Uri("https://integrate.api.nvidia.com/v1/") },
            Options.Create(new NvidiaOptions
            {
                ApiKey = "nvapi-test",
                ChatModel = "nvidia/nemotron-3-ultra-550b-a55b",
                MaxOutputTokens = 512,
            }),
            NullLogger<NvidiaChatClient>.Instance);

    private static async Task<List<string>> CollectAsync(NvidiaChatClient client)
    {
        var tokens = new List<string>();
        await foreach (var token in client.StreamCompletionAsync([]))
        {
            tokens.Add(token);
        }
        return tokens;
    }

    [Fact]
    public async Task StreamCompletionAsync_YieldsContentDeltas_AndSkipsOtherDeltaFields()
    {
        var handler = new SequenceHandler(Sse(
            """data: {"choices":[{"index":0,"delta":{"role":"assistant","reasoning_content":"thinking"},"finish_reason":null}]}""",
            """data: {"choices":[{"index":0,"delta":{"content":"The answer"},"finish_reason":null}]}""",
            """data: {"choices":[{"index":0,"delta":{"content":" is 90 days"},"finish_reason":null}]}""",
            """data: {"choices":[{"index":0,"delta":{},"finish_reason":"stop"}]}""",
            "data: [DONE]"));

        var tokens = await CollectAsync(CreateClient(handler));

        Assert.Equal(["The answer", " is 90 days"], tokens);
    }

    [Fact]
    public async Task StreamCompletionAsync_CompletesSilently_WhenStreamHasNoContent()
    {
        var handler = new SequenceHandler(Sse(
            """data: {"choices":[{"index":0,"delta":{"role":"assistant","reasoning_content":"thinking"},"finish_reason":null}]}""",
            """data: {"choices":[{"index":0,"delta":{},"finish_reason":"length"}]}""",
            "data: [DONE]"));

        var tokens = await CollectAsync(CreateClient(handler));

        Assert.Empty(tokens);
    }

    [Fact]
    public async Task StreamCompletionAsync_Throws_OnMidStreamErrorObject()
    {
        var handler = new SequenceHandler(Sse(
            """data: {"error":{"type":"upstream_error","message":"model is overloaded"}}"""));

        var exception = await Assert.ThrowsAsync<HttpRequestException>(
            () => CollectAsync(CreateClient(handler)));

        Assert.Contains("model is overloaded", exception.Message);
    }

    [Fact]
    public async Task StreamCompletionAsync_Throws_OnMidStreamErrorString()
    {
        var handler = new SequenceHandler(Sse("""data: {"error":"rate limit exceeded"}"""));

        var exception = await Assert.ThrowsAsync<HttpRequestException>(
            () => CollectAsync(CreateClient(handler)));

        Assert.Contains("rate limit exceeded", exception.Message);
    }
}
