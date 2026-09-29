using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pgvector;
using TecAssist.Application.Abstractions;
using TecAssist.Application.Options;
using TecAssist.Application.Persistence;
using TecAssist.Domain.Documents;

namespace TecAssist.Application.Rag;

public sealed class IngestionPipeline(
    ITecAssistDbContext db,
    IChunker chunker,
    IEmbeddingClient embeddingClient,
    IOptions<RagOptions> options,
    ILogger<IngestionPipeline> logger)
{
    public async Task ProcessAsync(IngestionJob job, CancellationToken cancellationToken)
    {
        var document = await db.Documents.FirstOrDefaultAsync(d => d.Id == job.DocumentId, cancellationToken);
        if (document is null)
        {
            logger.LogWarning("Skipping ingestion job for unknown document {DocumentId}", job.DocumentId);
            return;
        }

        document.Status = DocumentStatus.Processing;
        await db.SaveChangesAsync(cancellationToken);

        try
        {
            var chunks = chunker.Chunk(job.Text);
            if (chunks.Count == 0)
            {
                throw new InvalidOperationException("Chunking produced no content for the document.");
            }

            var batchSize = Math.Max(1, options.Value.EmbeddingBatchSize);
            for (var offset = 0; offset < chunks.Count; offset += batchSize)
            {
                var batch = chunks.Skip(offset).Take(batchSize).ToList();
                var texts = batch.Select(chunk => chunk.Content).ToList();
                var embeddings = await embeddingClient.EmbedAsync(texts, EmbeddingPurpose.Passage, cancellationToken);
                if (embeddings.Count != batch.Count)
                {
                    throw new InvalidOperationException("The embedding provider returned an unexpected number of vectors.");
                }

                for (var index = 0; index < batch.Count; index++)
                {
                    db.DocumentChunks.Add(new DocumentChunk
                    {
                        DocumentId = document.Id,
                        ChunkIndex = offset + index,
                        Content = batch[index].Content,
                        TokenCount = batch[index].TokenCount,
                        Embedding = new Vector(embeddings[index])
                    });
                }
            }

            document.Status = DocumentStatus.Ready;
            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Ingested document {DocumentId} into {ChunkCount} chunks", document.Id, chunks.Count);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Ingestion failed for document {DocumentId}", document.Id);
            document.Status = DocumentStatus.Failed;
            await db.SaveChangesAsync(CancellationToken.None);
        }
    }
}
