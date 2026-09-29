using Microsoft.EntityFrameworkCore;
using TecAssist.Application.Common;
using TecAssist.Application.Contracts;
using TecAssist.Application.Persistence;
using TecAssist.Domain.Conversations;

namespace TecAssist.Application.Conversations;

public sealed class ConversationService(ITecAssistDbContext db, ICurrentUser currentUser)
{
    public async Task<ConversationResponse> CreateAsync(string? title, CancellationToken cancellationToken = default)
    {
        var userId = currentUser.RequireUserId();
        var conversation = new Conversation
        {
            UserId = userId,
            Title = NormalizeTitle(title)
        };
        db.Conversations.Add(conversation);
        await db.SaveChangesAsync(cancellationToken);
        return ToResponse(conversation, messageCount: 0, lastMessageAt: null);
    }

    public async Task<IReadOnlyList<ConversationResponse>> ListAsync(CancellationToken cancellationToken = default)
    {
        var userId = currentUser.RequireUserId();
        var conversations = await db.Conversations
            .AsNoTracking()
            .Where(conversation => conversation.UserId == userId)
            .OrderByDescending(conversation => conversation.CreatedAt)
            .ToListAsync(cancellationToken);

        var conversationIds = conversations.Select(conversation => conversation.Id).ToList();
        var stats = await db.Messages
            .AsNoTracking()
            .Where(message => conversationIds.Contains(message.ConversationId))
            .GroupBy(message => message.ConversationId)
            .Select(group => new { group.Key, Count = group.Count(), LastAt = group.Max(message => (DateTime?)message.CreatedAt) })
            .ToListAsync(cancellationToken);

        var statMap = stats.ToDictionary(item => item.Key, item => (item.Count, item.LastAt));
        return conversations
            .Select(conversation =>
            {
                var (count, lastAt) = statMap.GetValueOrDefault(conversation.Id);
                return ToResponse(conversation, count, lastAt);
            })
            .ToList();
    }

    public async Task<ConversationResponse?> RenameAsync(Guid id, string title, CancellationToken cancellationToken = default)
    {
        var userId = currentUser.RequireUserId();
        var conversation = await db.Conversations
            .FirstOrDefaultAsync(conversation => conversation.Id == id && conversation.UserId == userId, cancellationToken);
        if (conversation is null)
        {
            return null;
        }

        conversation.Title = NormalizeTitle(title);
        await db.SaveChangesAsync(cancellationToken);
        return ToResponse(conversation, messageCount: 0, lastMessageAt: null);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var userId = currentUser.RequireUserId();
        var conversation = await db.Conversations
            .FirstOrDefaultAsync(conversation => conversation.Id == id && conversation.UserId == userId, cancellationToken);
        if (conversation is null)
        {
            return false;
        }

        db.Conversations.Remove(conversation);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<MessageResponse>?> GetMessagesAsync(Guid conversationId, CancellationToken cancellationToken = default)
    {
        var userId = currentUser.RequireUserId();
        var exists = await db.Conversations
            .AnyAsync(conversation => conversation.Id == conversationId && conversation.UserId == userId, cancellationToken);
        if (!exists)
        {
            return null;
        }

        var messages = await db.Messages
            .AsNoTracking()
            .Include(message => message.Citations)
                .ThenInclude(citation => citation.Chunk.Document)
            .Where(message => message.ConversationId == conversationId)
            .OrderBy(message => message.CreatedAt)
            .ToListAsync(cancellationToken);

        return messages
            .Select(message => new MessageResponse(
                message.Id,
                message.Role.ToString().ToLowerInvariant(),
                message.Content,
                message.CreatedAt,
                [.. message.Citations.Select(citation => new CitationResponse(
                    citation.ChunkId,
                    citation.Chunk.Document.Title,
                    citation.Chunk.Content.Truncate(200),
                    0))]))
            .ToList();
    }

    private static string? NormalizeTitle(string? title)
    {
        return string.IsNullOrWhiteSpace(title) ? null : title.Trim();
    }

    private static ConversationResponse ToResponse(Conversation conversation, int messageCount, DateTime? lastMessageAt) => new(
        conversation.Id,
        conversation.Title,
        conversation.CreatedAt,
        messageCount,
        lastMessageAt);
}
