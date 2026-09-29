using Microsoft.EntityFrameworkCore;
using TecAssist.Application.Persistence;
using TecAssist.Domain.Conversations;
using TecAssist.Domain.Documents;

namespace TecAssist.Infrastructure.Persistence;

public class TecAssistDbContext : DbContext, ITecAssistDbContext
{
    public TecAssistDbContext(DbContextOptions<TecAssistDbContext> options)
        : base(options)
    {
    }

    public DbSet<Document> Documents => Set<Document>();

    public DbSet<DocumentChunk> DocumentChunks => Set<DocumentChunk>();

    public DbSet<Conversation> Conversations => Set<Conversation>();

    public DbSet<Message> Messages => Set<Message>();

    public DbSet<MessageCitation> MessageCitations => Set<MessageCitation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasPostgresExtension("vector");

        modelBuilder.Entity<Document>(entity =>
        {
            entity.ToTable("documents");
            entity.HasKey(document => document.Id);
            entity.Property(document => document.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(document => document.Title).IsRequired();
            entity.Property(document => document.SourceType)
                .HasConversion(
                    value => value.ToString().ToLowerInvariant(),
                    value => Enum.Parse<SourceType>(value, true))
                .HasMaxLength(16);
            entity.Property(document => document.Status)
                .HasConversion(
                    value => value.ToString().ToLowerInvariant(),
                    value => Enum.Parse<DocumentStatus>(value, true))
                .HasMaxLength(16);
            entity.Property(document => document.CreatedAt).HasDefaultValueSql("now()");
            entity.HasMany(document => document.Chunks)
                .WithOne(chunk => chunk.Document)
                .HasForeignKey(chunk => chunk.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(document => document.UserId);
        });

        modelBuilder.Entity<DocumentChunk>(entity =>
        {
            entity.ToTable("document_chunks");
            entity.HasKey(chunk => chunk.Id);
            entity.Property(chunk => chunk.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(chunk => chunk.Content).IsRequired();
            entity.Property(chunk => chunk.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(chunk => chunk.Embedding).HasColumnType("vector(2048)");
            entity.HasIndex(chunk => chunk.DocumentId);
        });

        modelBuilder.Entity<Conversation>(entity =>
        {
            entity.ToTable("conversations");
            entity.HasKey(conversation => conversation.Id);
            entity.Property(conversation => conversation.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(conversation => conversation.CreatedAt).HasDefaultValueSql("now()");
            entity.HasMany(conversation => conversation.Messages)
                .WithOne()
                .HasForeignKey(message => message.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(conversation => conversation.UserId);
        });

        modelBuilder.Entity<Message>(entity =>
        {
            entity.ToTable("messages");
            entity.HasKey(message => message.Id);
            entity.Property(message => message.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(message => message.Role)
                .HasConversion(
                    value => value.ToString().ToLowerInvariant(),
                    value => Enum.Parse<MessageRole>(value, true))
                .HasMaxLength(16);
            entity.Property(message => message.Content).IsRequired();
            entity.Property(message => message.CreatedAt).HasDefaultValueSql("now()");
            entity.HasMany(message => message.Citations)
                .WithOne(citation => citation.Message)
                .HasForeignKey(citation => citation.MessageId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(message => message.ConversationId);
        });

        modelBuilder.Entity<MessageCitation>(entity =>
        {
            entity.ToTable("message_citations");
            entity.HasKey(citation => new { citation.MessageId, citation.ChunkId });
            entity.HasOne(citation => citation.Chunk)
                .WithMany()
                .HasForeignKey(citation => citation.ChunkId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
