using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;
using TecAssist.Application.Abstractions;
using TecAssist.Application.Persistence;

namespace TecAssist.Infrastructure.Search;

public sealed class PgVectorChunkSearcher(ITecAssistDbContext db, IEmbeddingClient embeddingClient) : IChunkSearcher
{
    public async Task<IReadOnlyList<ChunkSearchResult>> SearchAsync(
        Guid userId,
        string query,
        int topK,
        CancellationToken cancellationToken = default)
    {
        var queryEmbeddings = await embeddingClient.EmbedAsync([query], EmbeddingPurpose.Query, cancellationToken);
        var queryVector = queryEmbeddings[0];
        var vector = new Vector(queryVector);

        var rows = await db.DocumentChunks
            .AsNoTracking()
            .Where(chunk => chunk.Document.UserId == userId && chunk.Embedding != null)
            .OrderBy(chunk => chunk.Embedding!.CosineDistance(vector))
            .Take(topK)
            .Select(chunk => new
            {
                chunk.Id,
                chunk.DocumentId,
                Title = chunk.Document.Title,
                chunk.Content,
                chunk.Embedding
            })
            .ToListAsync(cancellationToken);

        return [.. rows.Select(row => new ChunkSearchResult(
            row.Id,
            row.DocumentId,
            row.Title,
            row.Content,
            CosineSimilarity(queryVector, row.Embedding)))];
    }

    private static double CosineSimilarity(float[] left, Vector? right)
    {
        if (right is null)
        {
            return 0;
        }

        var other = right.ToArray();
        double dotProduct = 0;
        double leftNorm = 0;
        double rightNorm = 0;

        for (var index = 0; index < left.Length; index++)
        {
            dotProduct += left[index] * other[index];
            leftNorm += left[index] * left[index];
            rightNorm += other[index] * other[index];
        }

        if (leftNorm == 0 || rightNorm == 0)
        {
            return 0;
        }

        return dotProduct / (Math.Sqrt(leftNorm) * Math.Sqrt(rightNorm));
    }
}
