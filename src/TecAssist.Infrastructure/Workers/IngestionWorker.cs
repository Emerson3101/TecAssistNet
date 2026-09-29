using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TecAssist.Application.Abstractions;
using TecAssist.Application.Persistence;
using TecAssist.Application.Rag;
using TecAssist.Domain.Documents;

namespace TecAssist.Infrastructure.Workers;

public sealed class IngestionWorker(
    IIngestionQueue ingestionQueue,
    IServiceScopeFactory scopeFactory,
    ILogger<IngestionWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await MarkOrphanedDocumentsAsFailedAsync(stoppingToken);
        logger.LogInformation("Ingestion worker started");

        await foreach (var job in ingestionQueue.DequeueAllAsync(stoppingToken))
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var pipeline = scope.ServiceProvider.GetRequiredService<IngestionPipeline>();
                await pipeline.ProcessAsync(job, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Unhandled ingestion failure for document {DocumentId}", job.DocumentId);
            }
        }

        logger.LogInformation("Ingestion worker stopped");
    }

    private async Task MarkOrphanedDocumentsAsFailedAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ITecAssistDbContext>();
            var orphaned = await db.Documents
                .Where(document => document.Status == DocumentStatus.Pending)
                .ToListAsync(cancellationToken);

            foreach (var document in orphaned)
            {
                document.Status = DocumentStatus.Failed;
            }

            if (orphaned.Count > 0)
            {
                await db.SaveChangesAsync(cancellationToken);
                logger.LogWarning(
                    "Marked {Count} orphaned pending document(s) as failed at startup",
                    orphaned.Count);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not mark orphaned pending documents as failed at startup");
        }
    }
}
