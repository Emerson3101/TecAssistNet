using TecAssist.Domain.Documents;

namespace TecAssist.Domain.Conversations;

public class MessageCitation
{
    public Guid MessageId { get; set; }

    public Guid ChunkId { get; set; }

    public Message Message { get; set; } = null!;

    public DocumentChunk Chunk { get; set; } = null!;
}
