namespace TecAssist.Application.Contracts;

public abstract record ChatStreamEvent;

public sealed record TokenEvent(string Text) : ChatStreamEvent;

public sealed record CitationsEvent(IReadOnlyList<CitationResponse> Citations) : ChatStreamEvent;

public sealed record CompletedEvent(Guid UserMessageId, Guid AssistantMessageId) : ChatStreamEvent;

public sealed record ChatMessageStream(Guid UserMessageId, IAsyncEnumerable<ChatStreamEvent> Events);
