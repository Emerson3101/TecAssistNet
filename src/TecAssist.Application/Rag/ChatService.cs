using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TecAssist.Application.Abstractions;
using TecAssist.Application.Common;
using TecAssist.Application.Contracts;
using TecAssist.Application.Options;
using TecAssist.Application.Persistence;
using TecAssist.Domain.Conversations;

namespace TecAssist.Application.Rag;

public sealed class ChatService(
    ITecAssistDbContext db,
    ICurrentUser currentUser,
    IChunkSearcher chunkSearcher,
    IChatClient chatClient,
    PromptBuilder promptBuilder,
    IOptions<RagOptions> options)
{
    private static readonly Regex WhitespacePattern = new(@"\s+", RegexOptions.Compiled);

    public async Task<ChatMessageStream> StartMessageStreamAsync(
        Guid conversationId,
        string content,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUser.RequireUserId();
        var conversation = await db.Conversations
            .FirstOrDefaultAsync(c => c.Id == conversationId && c.UserId == userId, cancellationToken)
            ?? throw new ResourceNotFoundException($"Conversation '{conversationId}' was not found.");

        var userMessage = new Message
        {
            ConversationId = conversationId,
            Role = MessageRole.User,
            Content = content
        };
        db.Messages.Add(userMessage);
        await db.SaveChangesAsync(cancellationToken);

        IReadOnlyList<ChunkSearchResult> context;
        try
        {
            context = await chunkSearcher.SearchAsync(userId, content, options.Value.TopK, cancellationToken);
        }
        catch (Exception exception)
        {
            throw new ChatGenerationException("Failed to retrieve relevant context for the question.", exception);
        }

        var history = await db.Messages
            .AsNoTracking()
            .Where(message => message.ConversationId == conversationId && message.Id != userMessage.Id)
            .OrderByDescending(message => message.CreatedAt)
            .Take(options.Value.MaxHistoryMessages)
            .ToListAsync(cancellationToken);
        history.Reverse();

        var prompt = promptBuilder.Build(context, history, content);

        return new ChatMessageStream(
            userMessage.Id,
            StreamAnswerAsync(conversationId, userMessage.Id, context, prompt, cancellationToken));
    }

    public async Task<string?> GenerateTitleAsync(
        Guid conversationId,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUser.RequireUserId();
        var conversation = await db.Conversations
            .FirstOrDefaultAsync(c => c.Id == conversationId && c.UserId == userId, cancellationToken)
            ?? throw new ResourceNotFoundException($"Conversation '{conversationId}' was not found.");

        if (!string.IsNullOrWhiteSpace(conversation.Title))
        {
            return conversation.Title;
        }

        var firstExchange = await db.Messages
            .AsNoTracking()
            .Where(message => message.ConversationId == conversationId)
            .OrderBy(message => message.CreatedAt)
            .Take(2)
            .ToListAsync(cancellationToken);

        if (firstExchange.Count == 0)
        {
            return conversation.Title;
        }

        var prompt = new List<ChatMessage>
        {
            new(
                ChatRoles.System,
                "You name chat conversations. Reply with a concise title of at most six words. No quotes, no trailing punctuation, no explanation - only the title."),
            new(
                ChatRoles.User,
                $"Question: {firstExchange[0].Content}\n\nAnswer: {(firstExchange.Count > 1 ? firstExchange[1].Content.Truncate(400) : string.Empty)}"),
        };

        var builder = new StringBuilder();
        await foreach (var token in chatClient.StreamCompletionAsync(prompt, cancellationToken))
        {
            builder.Append(token);
        }

        var title = CleanTitle(builder.ToString(), options.Value.ConversationTitleMaxLength);
        if (string.IsNullOrWhiteSpace(title))
        {
            return conversation.Title;
        }

        conversation.Title = title;
        await db.SaveChangesAsync(cancellationToken);
        return title;
    }

    private static string CleanTitle(string raw, int maxLength)
    {
        var title = WhitespacePattern.Replace(raw, " ").Trim();
        title = title.Trim('"', '\'', '`', '*', ' ', '.');
        if (title.Length > maxLength)
        {
            title = title[..maxLength].Trim();
        }

        return title;
    }

    private async IAsyncEnumerable<ChatStreamEvent> StreamAnswerAsync(
        Guid conversationId,
        Guid userMessageId,
        IReadOnlyList<ChunkSearchResult> context,
        IReadOnlyList<ChatMessage> prompt,
        CancellationToken cancellationToken)
    {
        var answer = new StringBuilder();

        await using var enumerator = chatClient
            .StreamCompletionAsync(prompt, cancellationToken)
            .GetAsyncEnumerator(cancellationToken);

        while (true)
        {
            string token;
            try
            {
                if (!await enumerator.MoveNextAsync())
                {
                    break;
                }

                token = enumerator.Current;
            }
            catch (Exception exception)
            {
                throw new ChatGenerationException("The chat completion stream failed.", exception);
            }

            answer.Append(token);
            yield return new TokenEvent(token);
        }

        var answerText = answer.ToString();
        if (string.IsNullOrWhiteSpace(answerText))
        {
            throw new ChatGenerationException("The model returned an empty answer. Please try again.");
        }

        var citedIndices = CitationParser.ExtractCitedIndices(answerText, context.Count);
        var citations = citedIndices.Count == 0
            ? context.ToList()
            : citedIndices.Select(index => context[index - 1]).ToList();

        var assistantMessage = new Message
        {
            ConversationId = conversationId,
            Role = MessageRole.Assistant,
            Content = answerText
        };
        foreach (var citation in citations)
        {
            assistantMessage.Citations.Add(new MessageCitation { ChunkId = citation.ChunkId });
        }

        db.Messages.Add(assistantMessage);
        await db.SaveChangesAsync(cancellationToken);

        yield return new CitationsEvent(
            [.. citations.Select(citation => new CitationResponse(
                citation.ChunkId,
                citation.DocumentTitle,
                citation.Content.Truncate(200),
                citation.Score))]);

        yield return new CompletedEvent(userMessageId, assistantMessage.Id);
    }
}
