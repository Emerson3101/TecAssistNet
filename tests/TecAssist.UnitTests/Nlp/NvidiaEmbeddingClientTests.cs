using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TecAssist.Application.Abstractions;
using TecAssist.Infrastructure.Nlp;

namespace TecAssist.UnitTests.Nlp;

public sealed class NvidiaEmbeddingClientTests
{
    private const string SuccessPayload =
        """
        {"data":[{"index":0,"embedding":[0.25,0.5]},{"index":1,"embedding":[0.75,0.25]}]}
        """;

    private sealed class SequenceHandler : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> _responses;

        public int RequestCount { get; private set; }

        public SequenceHandler(params HttpResponseMessage[] responses)
        {
            _responses = new Queue<HttpResponseMessage>(responses);
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestCount++;
            return Task.FromResult(_responses.Dequeue());
        }
    }

    private static HttpResponseMessage Status(HttpStatusCode statusCode) =>
        new(statusCode) { Content = new StringContent("""{"error": "transient"}""") };

    private static HttpResponseMessage Success() =>
        new(HttpStatusCode.OK) { Content = new StringContent(SuccessPayload) };

    private static NvidiaEmbeddingClient CreateClient(HttpMessageHandler handler) =>
        new(
            new HttpClient(handler) { BaseAddress = new Uri("https://integrate.api.nvidia.com/v1/") },
            Options.Create(new NvidiaOptions
            {
                ApiKey = "nvapi-test",
                EmbeddingModel = "nvidia/nemotron-3-embed-1b",
            }));

    [Fact]
    public async Task EmbedAsync_ReturnsVectors_OnFirstSuccess()
    {
        var handler = new SequenceHandler(Success());
        var client = CreateClient(handler);

        var vectors = await client.EmbedAsync(["one", "two"], EmbeddingPurpose.Passage);

        Assert.Equal(2, vectors.Count);
        Assert.Equal([0.25f, 0.5f], vectors[0]);
        Assert.Equal(1, handler.RequestCount);
    }

    [Fact]
    public async Task EmbedAsync_RetriesOn502_AndSucceeds()
    {
        var handler = new SequenceHandler(Status(HttpStatusCode.BadGateway), Success());
        var client = CreateClient(handler);

        var vectors = await client.EmbedAsync(["one", "two"], EmbeddingPurpose.Passage);

        Assert.Equal(2, vectors.Count);
        Assert.Equal(2, handler.RequestCount);
    }

    [Fact]
    public async Task EmbedAsync_RetriesTwice_OnRepeated5xx()
    {
        var handler = new SequenceHandler(
            Status(HttpStatusCode.ServiceUnavailable),
            Status(HttpStatusCode.GatewayTimeout),
            Success());
        var client = CreateClient(handler);

        var vectors = await client.EmbedAsync(["one", "two"], EmbeddingPurpose.Passage);

        Assert.Equal(2, vectors.Count);
        Assert.Equal(3, handler.RequestCount);
    }

    [Fact]
    public async Task EmbedAsync_Throws_AfterExhaustingRetries()
    {
        var handler = new SequenceHandler(
            Status(HttpStatusCode.BadGateway),
            Status(HttpStatusCode.BadGateway),
            Status(HttpStatusCode.BadGateway));
        var client = CreateClient(handler);

        await Assert.ThrowsAsync<HttpRequestException>(
            () => client.EmbedAsync(["one"], EmbeddingPurpose.Passage));

        Assert.Equal(3, handler.RequestCount);
    }

    [Fact]
    public async Task EmbedAsync_ThrowsImmediately_On400()
    {
        var handler = new SequenceHandler(Status(HttpStatusCode.BadRequest), Success());
        var client = CreateClient(handler);

        await Assert.ThrowsAsync<HttpRequestException>(
            () => client.EmbedAsync(["one"], EmbeddingPurpose.Passage));

        Assert.Equal(1, handler.RequestCount);
    }

    [Fact]
    public async Task EmbedAsync_ReturnsEmpty_ForNoTexts()
    {
        var handler = new SequenceHandler();
        var client = CreateClient(handler);

        var vectors = await client.EmbedAsync([], EmbeddingPurpose.Passage);

        Assert.Empty(vectors);
        Assert.Equal(0, handler.RequestCount);
    }
}
