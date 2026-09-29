namespace TecAssist.Application.Contracts;

public sealed record DocumentResponse(
    Guid Id,
    string Title,
    string SourceType,
    string Status,
    DateTime CreatedAt,
    int ChunkCount);

public sealed record ConversationResponse(
    Guid Id,
    string? Title,
    DateTime CreatedAt,
    int MessageCount,
    DateTime? LastMessageAt);

public sealed record CitationResponse(
    Guid ChunkId,
    string DocumentTitle,
    string Snippet,
    double Score);

public sealed record MessageResponse(
    Guid Id,
    string Role,
    string Content,
    DateTime CreatedAt,
    IReadOnlyList<CitationResponse> Citations);

public sealed record SendMessageResponse(
    Guid UserMessageId,
    Guid AssistantMessageId,
    string Answer,
    IReadOnlyList<CitationResponse> Citations);
