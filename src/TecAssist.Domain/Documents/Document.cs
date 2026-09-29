namespace TecAssist.Domain.Documents;

public class Document
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }

    public string Title { get; set; } = string.Empty;

    public SourceType SourceType { get; set; }

    public DocumentStatus Status { get; set; } = DocumentStatus.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<DocumentChunk> Chunks { get; set; } = [];
}
