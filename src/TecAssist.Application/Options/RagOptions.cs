namespace TecAssist.Application.Options;

public sealed class RagOptions
{
    public const string SectionName = "Rag";

    public int ChunkSizeTokens { get; set; } = 250;

    public int OverlapTokens { get; set; } = 40;

    public int TopK { get; set; } = 5;

    public int MaxHistoryMessages { get; set; } = 10;

    public int EmbeddingBatchSize { get; set; } = 2;

    public int IngestionQueueCapacity { get; set; } = 128;

    public int ConversationTitleMaxLength { get; set; } = 80;

    public int ChatStreamEmptyRetries { get; set; } = 1;

    public TimeSpan ChatStreamRetryDelay { get; set; } = TimeSpan.FromSeconds(1.5);
}
