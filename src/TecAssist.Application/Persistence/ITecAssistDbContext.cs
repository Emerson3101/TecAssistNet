using Microsoft.EntityFrameworkCore;
using TecAssist.Domain.Conversations;
using TecAssist.Domain.Documents;

namespace TecAssist.Application.Persistence;

public interface ITecAssistDbContext
{
    DbSet<Document> Documents { get; }

    DbSet<DocumentChunk> DocumentChunks { get; }

    DbSet<Conversation> Conversations { get; }

    DbSet<Message> Messages { get; }

    DbSet<MessageCitation> MessageCitations { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
