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
    public async Task<SendMessageResponse?> SendMessageAsync(
        Guid conversationId,
        string content,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUser.RequireUserId();
        var conversation = await db.Conversations
            .FirstOrDefaultAsync(c => c.Id == conversationId && c.UserId == userId, cancellationToken);
        if (conversation is null)
        {
            return null;
        }

        var userMessage = new Message
        {
            ConversationId = conversationId,
            Role = MessageRole.User,
            Content = content
        };
        db.Messages.Add(userMessage);
        if (string.IsNullOrWhiteSpace(conversation.Title))
        {
            conversation.Title = content.Truncate(options.Value.ConversationTitleMaxLength);
        }

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

        string answer;
        try
        {
            answer = await chatClient.CompleteAsync(prompt, cancellationToken);
        }
        catch (Exception exception)
        {
            throw new ChatGenerationException("The chat completion request failed.", exception);
        }

        var citedIndices = CitationParser.ExtractCitedIndices(answer, context.Count);
        var citations = citedIndices.Count == 0
            ? context.ToList()
            : citedIndices.Select(index => context[index - 1]).ToList();

        var assistantMessage = new Message
        {
            ConversationId = conversationId,
            Role = MessageRole.Assistant,
            Content = answer
        };
        foreach (var citation in citations)
        {
            assistantMessage.Citations.Add(new MessageCitation { ChunkId = citation.ChunkId });
        }

        db.Messages.Add(assistantMessage);
        await db.SaveChangesAsync(cancellationToken);

        return new SendMessageResponse(
            userMessage.Id,
            assistantMessage.Id,
            answer,
            [.. citations.Select(citation => new CitationResponse(
                citation.ChunkId,
                citation.DocumentTitle,
                citation.Content.Truncate(200),
                citation.Score))]);
    }
}
