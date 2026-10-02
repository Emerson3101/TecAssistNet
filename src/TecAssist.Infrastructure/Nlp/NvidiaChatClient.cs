using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TecAssist.Application.Abstractions;
using AppChatMessage = TecAssist.Application.Abstractions.ChatMessage;

namespace TecAssist.Infrastructure.Nlp;

public sealed class NvidiaChatClient(
    HttpClient httpClient,
    IOptions<NvidiaOptions> options,
    ILogger<NvidiaChatClient> logger) : IChatClient
{
    public async IAsyncEnumerable<string> StreamCompletionAsync(
        IReadOnlyList<AppChatMessage> messages,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.ApiKey) || string.IsNullOrWhiteSpace(settings.ChatModel))
        {
            throw new InvalidOperationException(
                "NVIDIA chat settings are not configured. Set Nvidia:ApiKey and Nvidia:ChatModel.");
        }

        var request = new ChatCompletionRequest(
            settings.ChatModel,
            [.. messages.Select(message => new WireChatMessage(message.Role, message.Content))],
            settings.MaxOutputTokens,
            Stream: true);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "chat/completions");
        httpRequest.Content = JsonContent.Create(request, options: Json.Options);

        using var response = await httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException(
                $"The NVIDIA chat endpoint returned {(int)response.StatusCode}: {Truncate(body)}");
        }

        using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(contentStream);

        var producedContent = false;
        string? finishReason = null;

        while (true)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (line is null)
            {
                break;
            }

            line = line.Trim();
            if (line.Length == 0 || !line.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var payload = line["data:".Length..].Trim();
            if (payload == "[DONE]")
            {
                break;
            }

            var update = JsonSerializer.Deserialize<ChatStreamUpdate>(payload, Json.Options);

            if (update?.Error is { } error)
            {
                throw new HttpRequestException(
                    $"The NVIDIA chat endpoint reported a mid-stream error: {Truncate(ExtractErrorMessage(error))}");
            }

            var choice = update?.Choices is { Count: > 0 } ? update.Choices[0] : null;
            if (choice?.FinishReason is not null)
            {
                finishReason = choice.FinishReason;
            }

            var deltaContent = choice?.Delta?.Content;
            if (!string.IsNullOrEmpty(deltaContent))
            {
                producedContent = true;
                yield return deltaContent;
            }
        }

        if (!producedContent)
        {
            logger.LogWarning(
                "The NVIDIA chat stream for model {Model} ended without any content (finish reason: {FinishReason}).",
                settings.ChatModel,
                finishReason ?? "unknown");
        }
    }

    private static string ExtractErrorMessage(JsonElement error) => error.ValueKind switch
    {
        JsonValueKind.String => error.GetString() ?? string.Empty,
        JsonValueKind.Object when error.TryGetProperty("message", out var message)
            && message.ValueKind == JsonValueKind.String => message.GetString() ?? string.Empty,
        _ => error.ToString(),
    };

    private static string Truncate(string body) => body.Length <= 500 ? body : body[..500];

    private static class Json
    {
        public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);
    }

    private sealed record ChatCompletionRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("messages")] IReadOnlyList<WireChatMessage> Messages,
        [property: JsonPropertyName("max_tokens")] int MaxTokens,
        [property: JsonPropertyName("stream")] bool Stream);

    private sealed record WireChatMessage(
        [property: JsonPropertyName("role")] string Role,
        [property: JsonPropertyName("content")] string Content);

    private sealed record ChatStreamUpdate(
        [property: JsonPropertyName("choices")] IReadOnlyList<WireChoice>? Choices,
        [property: JsonPropertyName("error")] JsonElement? Error);

    private sealed record WireChoice(
        [property: JsonPropertyName("delta")] WireDelta? Delta,
        [property: JsonPropertyName("finish_reason")] string? FinishReason);

    private sealed record WireDelta(
        [property: JsonPropertyName("content")] string? Content);
}
