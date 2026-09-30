using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Pgvector;
using TecAssist.Application.Abstractions;
using TecAssist.Application.Options;
using TecAssist.Application.Rag;
using TecAssist.Domain.Documents;
using TecAssist.UnitTests.Fakes;
using TecAssist.UnitTests.Persistence;

namespace TecAssist.UnitTests.Rag;

public sealed class IngestionPipelineTests : DatabaseTestBase
{
    private readonly Guid _userId = Guid.NewGuid();

    private async Task<(IngestionPipeline Pipeline, TestDbContext Context)> CreatePipelineAsync(
        IEmbeddingClient embeddingClient)
    {
        var context = await CreateContextAsync();
        var currentUser = new FakeCurrentUser(_userId);
        var pipeline = new IngestionPipeline(
            context,
            new TokenAwareChunker(Options.Create(new RagOptions())),
            embeddingClient,
            Options.Create(new RagOptions { EmbeddingBatchSize = 2 }),
            NullLogger<IngestionPipeline>.Instance);
        return (pipeline, context);
    }

    private async Task<Document> SeedDocumentAsync(TestDbContext context)
    {
        var document = new Document { UserId = _userId, Title = "Report", Status = DocumentStatus.Pending };
        context.Documents.Add(document);
        await context.SaveChangesAsync();
        return document;
    }

    [Fact]
    public async Task Process_MarksDocumentReady_AndStoresChunksWithEmbeddings()
    {
        var (pipeline, context) = await CreatePipelineAsync(new FakeEmbeddingClient(dimensions: 2048));
        var document = await SeedDocumentAsync(context);
        var text = "Alpha beta gamma delta.\n\nEpsilon zeta eta theta.";

        await pipeline.ProcessAsync(new IngestionJob(document.Id, text), CancellationToken.None);

        await context.Entry(document).ReloadAsync();
        Assert.Equal(DocumentStatus.Ready, document.Status);

        var chunks = await context.DocumentChunks
            .Where(chunk => chunk.DocumentId == document.Id)
            .OrderBy(chunk => chunk.ChunkIndex)
            .ToListAsync(CancellationToken.None);
        Assert.Single(chunks);
        Assert.NotNull(chunks[0].Embedding);
        Assert.Equal(2048, chunks[0].Embedding!.ToArray().Length);
        Assert.Equal(0, chunks[0].ChunkIndex);
    }

    [Fact]
    public async Task Process_MarksDocumentFailed_WhenEmbeddingFails()
    {
        var (pipeline, context) = await CreatePipelineAsync(
            new FakeEmbeddingClient(dimensions: 2048, throwOnCall: new InvalidOperationException("provider down")));
        var document = await SeedDocumentAsync(context);

        await pipeline.ProcessAsync(new IngestionJob(document.Id, "Some text content here."), CancellationToken.None);

        await context.Entry(document).ReloadAsync();
        Assert.Equal(DocumentStatus.Failed, document.Status);
        Assert.Empty(await context.DocumentChunks.ToListAsync(CancellationToken.None));
    }

    [Fact]
    public async Task Process_SkipsSilently_ForUnknownDocument()
    {
        var (pipeline, _) = await CreatePipelineAsync(new FakeEmbeddingClient(dimensions: 2048));

        await pipeline.ProcessAsync(new IngestionJob(Guid.NewGuid(), "text"), CancellationToken.None);
    }

    [Fact]
    public async Task Process_BatchesEmbeddingCalls_WhenChunksExceedBatchSize()
    {
        var (pipeline, context) = await CreatePipelineAsync(new FakeEmbeddingClient(dimensions: 8));
        var document = await SeedDocumentAsync(context);

        var paragraph = string.Join(' ', Enumerable.Range(0, 600).Select(index => $"w{index}"));
        await pipeline.ProcessAsync(new IngestionJob(document.Id, paragraph), CancellationToken.None);

        await context.Entry(document).ReloadAsync();
        Assert.Equal(DocumentStatus.Ready, document.Status);

        var chunkCount = await context.DocumentChunks.CountAsync(
            chunk => chunk.DocumentId == document.Id,
            CancellationToken.None);
        Assert.True(chunkCount >= 2);
    }
}
