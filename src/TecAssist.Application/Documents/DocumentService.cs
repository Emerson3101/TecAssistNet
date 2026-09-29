using Microsoft.EntityFrameworkCore;
using TecAssist.Application.Abstractions;
using TecAssist.Application.Common;
using TecAssist.Application.Contracts;
using TecAssist.Application.Persistence;
using TecAssist.Domain.Documents;

namespace TecAssist.Application.Documents;

public sealed class DocumentService(
    ITecAssistDbContext db,
    ICurrentUser currentUser,
    ITextExtractor textExtractor,
    IIngestionQueue ingestionQueue)
{
    public async Task<DocumentResponse> CreateFromFileAsync(
        string fileName,
        Stream content,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUser.RequireUserId();
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        var sourceType = extension switch
        {
            ".pdf" => SourceType.Pdf,
            ".md" or ".markdown" => SourceType.Markdown,
            ".txt" => SourceType.Text,
            _ => throw new AppValidationException($"Unsupported file type '{extension}'. Allowed types: .pdf, .md, .txt.")
        };

        var text = await textExtractor.ExtractAsync(sourceType, content, cancellationToken);
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new AppValidationException("No text could be extracted from the uploaded file. Scanned PDFs without a text layer are not supported.");
        }

        return await CreateAsync(userId, Path.GetFileNameWithoutExtension(fileName), sourceType, text, cancellationToken);
    }

    public async Task<DocumentResponse> CreateFromTextAsync(CreateTextDocumentRequest request, CancellationToken cancellationToken = default)
    {
        var userId = currentUser.RequireUserId();
        var sourceType = request.SourceType == "markdown" ? SourceType.Markdown : SourceType.Text;
        return await CreateAsync(userId, request.Title, sourceType, request.Content, cancellationToken);
    }

    private async Task<DocumentResponse> CreateAsync(
        Guid userId,
        string title,
        SourceType sourceType,
        string text,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new AppValidationException("A document title is required.");
        }

        var document = new Document
        {
            UserId = userId,
            Title = title.Trim(),
            SourceType = sourceType,
            Status = DocumentStatus.Pending
        };
        db.Documents.Add(document);
        await db.SaveChangesAsync(cancellationToken);

        if (!ingestionQueue.TryEnqueue(new IngestionJob(document.Id, text)))
        {
            document.Status = DocumentStatus.Failed;
            await db.SaveChangesAsync(CancellationToken.None);
            throw new ServiceUnavailableException("The ingestion queue is full. Please retry in a moment.");
        }

        return ToResponse(document, chunkCount: 0);
    }

    public async Task<IReadOnlyList<DocumentResponse>> ListAsync(CancellationToken cancellationToken = default)
    {
        var userId = currentUser.RequireUserId();
        var documents = await db.Documents
            .AsNoTracking()
            .Where(document => document.UserId == userId)
            .OrderByDescending(document => document.CreatedAt)
            .ToListAsync(cancellationToken);

        var documentIds = documents.Select(document => document.Id).ToList();
        var counts = await db.DocumentChunks
            .AsNoTracking()
            .Where(chunk => documentIds.Contains(chunk.DocumentId))
            .GroupBy(chunk => chunk.DocumentId)
            .Select(group => new { group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken);

        var countMap = counts.ToDictionary(item => item.Key, item => item.Count);
        return documents
            .Select(document => ToResponse(document, countMap.GetValueOrDefault(document.Id)))
            .ToList();
    }

    public async Task<DocumentResponse?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var userId = currentUser.RequireUserId();
        var document = await db.Documents
            .AsNoTracking()
            .FirstOrDefaultAsync(document => document.Id == id && document.UserId == userId, cancellationToken);
        if (document is null)
        {
            return null;
        }

        var chunkCount = await db.DocumentChunks
            .AsNoTracking()
            .CountAsync(chunk => chunk.DocumentId == id, cancellationToken);
        return ToResponse(document, chunkCount);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var userId = currentUser.RequireUserId();
        var document = await db.Documents
            .FirstOrDefaultAsync(document => document.Id == id && document.UserId == userId, cancellationToken);
        if (document is null)
        {
            return false;
        }

        db.Documents.Remove(document);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static DocumentResponse ToResponse(Document document, int chunkCount) => new(
        document.Id,
        document.Title,
        document.SourceType.ToString().ToLowerInvariant(),
        document.Status.ToString().ToLowerInvariant(),
        document.CreatedAt,
        chunkCount);
}
