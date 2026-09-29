namespace TecAssist.Application.Options;

public sealed class RagOptions
{
    public const string SectionName = "Rag";

    public int ChunkSizeTokens { get; set; } = 500;

    public int OverlapTokens { get; set; } = 50;

    public int TopK { get; set; } = 5;

    public int MaxHistoryMessages { get; set; } = 10;

    public int EmbeddingBatchSize { get; set; } = 32;

    public int IngestionQueueCapacity { get; set; } = 128;

    public int ConversationTitleMaxLength { get; set; } = 80;
}
