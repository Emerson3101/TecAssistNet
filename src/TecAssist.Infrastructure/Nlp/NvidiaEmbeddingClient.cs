using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using TecAssist.Application.Abstractions;

namespace TecAssist.Infrastructure.Nlp;

public sealed class NvidiaEmbeddingClient(HttpClient httpClient, IOptions<NvidiaOptions> options) : IEmbeddingClient
{
    public async Task<IReadOnlyList<float[]>> EmbedAsync(
        IReadOnlyList<string> texts,
        EmbeddingPurpose purpose,
        CancellationToken cancellationToken = default)
    {
        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.ApiKey) || string.IsNullOrWhiteSpace(settings.EmbeddingModel))
        {
            throw new InvalidOperationException(
                "NVIDIA embeddings settings are not configured. Set Nvidia:ApiKey and Nvidia:EmbeddingModel.");
        }

        if (texts.Count == 0)
        {
            return [];
        }

        var request = new EmbeddingsRequest(
            [.. texts],
            settings.EmbeddingModel,
            purpose == EmbeddingPurpose.Query ? "query" : "passage",
            "float");

        using var response = await httpClient.PostAsJsonAsync("embeddings", request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException(
                $"The NVIDIA embeddings endpoint returned {(int)response.StatusCode}: {Truncate(body)}");
        }

        var payload = await response.Content.ReadFromJsonAsync<EmbeddingsResponse>(cancellationToken);
        var items = payload?.Data;
        if (items is null || items.Count != texts.Count)
        {
            throw new HttpRequestException("The NVIDIA embeddings endpoint returned an unexpected payload.");
        }

        return [.. items
            .OrderBy(item => item.Index)
            .Select(item => item.Embedding)];
    }

    private static string Truncate(string body) => body.Length <= 500 ? body : body[..500];

    private sealed record EmbeddingsRequest(
        [property: JsonPropertyName("input")] string[] Input,
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("input_type")] string InputType,
        [property: JsonPropertyName("encoding_format")] string EncodingFormat);

    private sealed record EmbeddingsResponse(
        [property: JsonPropertyName("data")] IReadOnlyList<EmbeddingsItem> Data);

    private sealed record EmbeddingsItem(
        [property: JsonPropertyName("index")] int Index,
        [property: JsonPropertyName("embedding")] float[] Embedding);
}
