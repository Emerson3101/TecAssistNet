using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TecAssist.Application.Abstractions;
using TecAssist.Application.Options;

namespace TecAssist.Infrastructure.Nlp;

public sealed class ResilientChatClient(
    IChatClient inner,
    ILogger<ResilientChatClient> logger,
    IOptions<RagOptions> options) : IChatClient
{
    public async IAsyncEnumerable<string> StreamCompletionAsync(
        IReadOnlyList<ChatMessage> messages,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var settings = options.Value;

        for (var attempt = 1; ; attempt++)
        {
            var producedContent = false;

            await using var enumerator = inner
                .StreamCompletionAsync(messages, cancellationToken)
                .GetAsyncEnumerator(cancellationToken);

            while (true)
            {
                if (!await enumerator.MoveNextAsync())
                {
                    break;
                }

                var token = enumerator.Current;
                if (!string.IsNullOrEmpty(token))
                {
                    producedContent = true;
                }

                yield return token;
            }

            if (producedContent)
            {
                yield break;
            }

            if (attempt > settings.ChatStreamEmptyRetries)
            {
                logger.LogWarning(
                    "The chat completion stream produced no content after {Attempts} attempt(s); giving up.",
                    attempt);
                yield break;
            }

            logger.LogWarning(
                "The chat completion stream produced no content on attempt {Attempt}; retrying.",
                attempt);

            if (settings.ChatStreamRetryDelay > TimeSpan.Zero)
            {
                await Task.Delay(settings.ChatStreamRetryDelay, cancellationToken);
            }
        }
    }
}
