using Microsoft.EntityFrameworkCore;
using Pgvector;
using TecAssist.Application.Persistence;
using TecAssist.Domain.Conversations;
using TecAssist.Domain.Documents;

namespace TecAssist.UnitTests.Persistence;

public sealed class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options), ITecAssistDbContext
{
    public DbSet<Document> Documents => Set<Document>();

    public DbSet<DocumentChunk> DocumentChunks => Set<DocumentChunk>();

    public DbSet<Conversation> Conversations => Set<Conversation>();

    public DbSet<Message> Messages => Set<Message>();

    public DbSet<MessageCitation> MessageCitations => Set<MessageCitation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Document>(entity =>
        {
            entity.HasKey(document => document.Id);
            entity.HasMany(document => document.Chunks)
                .WithOne(chunk => chunk.Document)
                .HasForeignKey(chunk => chunk.DocumentId);
        });

        modelBuilder.Entity<DocumentChunk>(entity =>
        {
            entity.HasKey(chunk => chunk.Id);
            entity.Property(chunk => chunk.Embedding)
                .HasConversion(
                    vector => vector.ToString(),
                    value => new Vector(value));
        });

        modelBuilder.Entity<Conversation>(entity =>
        {
            entity.HasKey(conversation => conversation.Id);
            entity.HasMany(conversation => conversation.Messages)
                .WithOne()
                .HasForeignKey(message => message.ConversationId);
        });

        modelBuilder.Entity<Message>(entity =>
        {
            entity.HasKey(message => message.Id);
            entity.HasMany(message => message.Citations)
                .WithOne(citation => citation.Message)
                .HasForeignKey(citation => citation.MessageId);
        });

        modelBuilder.Entity<MessageCitation>(entity =>
        {
            entity.HasKey(citation => new { citation.MessageId, citation.ChunkId });
            entity.HasOne(citation => citation.Chunk)
                .WithMany()
                .HasForeignKey(citation => citation.ChunkId);
        });
    }
}
