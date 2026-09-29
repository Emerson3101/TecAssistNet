using System.Runtime.CompilerServices;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;
using TecAssist.Application.Abstractions;
using AppChatMessage = TecAssist.Application.Abstractions.ChatMessage;
using SdkChatMessage = OpenAI.Chat.ChatMessage;

namespace TecAssist.Infrastructure.Nlp;

public sealed class NvidiaChatClient : IChatClient
{
    private readonly ChatClient _chatClient;
    private readonly ChatCompletionOptions _completionOptions;

    public NvidiaChatClient(IOptions<NvidiaOptions> options)
    {
        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.ApiKey) || string.IsNullOrWhiteSpace(settings.ChatModel))
        {
            throw new InvalidOperationException(
                "NVIDIA chat settings are not configured. Set Nvidia:ApiKey and Nvidia:ChatModel.");
        }

        var clientOptions = new OpenAIClientOptions
        {
            Endpoint = new Uri(settings.BaseUrl.TrimEnd('/') + "/")
        };
        var openAiClient = new OpenAIClient(new ApiKeyCredential(settings.ApiKey), clientOptions);
        _chatClient = openAiClient.GetChatClient(settings.ChatModel);
        _completionOptions = new ChatCompletionOptions
        {
            MaxOutputTokenCount = settings.MaxOutputTokens
        };
    }

    public async Task<string> CompleteAsync(
        IReadOnlyList<AppChatMessage> messages,
        CancellationToken cancellationToken = default)
    {
        var result = await _chatClient.CompleteChatAsync(ToSdkMessages(messages), _completionOptions, cancellationToken);
        return ExtractText(result.Value.Content);
    }

    public async IAsyncEnumerable<string> StreamCompletionAsync(
        IReadOnlyList<AppChatMessage> messages,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var update in _chatClient.CompleteChatStreamingAsync(ToSdkMessages(messages), _completionOptions, cancellationToken))
        {
            if (update.ContentUpdate.Count > 0)
            {
                yield return update.ContentUpdate[0].Text;
            }
        }
    }

    private static List<SdkChatMessage> ToSdkMessages(IReadOnlyList<AppChatMessage> messages) =>
        [.. messages.Select(message => (SdkChatMessage)(message.Role switch
        {
            ChatRoles.System => new SystemChatMessage(message.Content),
            ChatRoles.Assistant => new AssistantChatMessage(message.Content),
            _ => new UserChatMessage(message.Content)
        }))];

    private static string ExtractText(ChatMessageContent content) =>
        content.Count == 0 ? string.Empty : string.Concat(content.Select(part => part.Text));
}
