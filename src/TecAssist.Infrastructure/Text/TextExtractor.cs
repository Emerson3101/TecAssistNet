using TecAssist.Application.Abstractions;
using TecAssist.Domain.Documents;
using UglyToad.PdfPig;

namespace TecAssist.Infrastructure.Text;

public sealed class TextExtractor : ITextExtractor
{
    public async Task<string> ExtractAsync(
        SourceType sourceType,
        Stream content,
        CancellationToken cancellationToken = default)
    {
        return sourceType switch
        {
            SourceType.Text or SourceType.Markdown => await ExtractPlainTextAsync(content, cancellationToken),
            SourceType.Pdf => await ExtractPdfTextAsync(content, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(sourceType), sourceType, "Unsupported source type.")
        };
    }

    private static async Task<string> ExtractPlainTextAsync(Stream content, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(content, leaveOpen: true);
        return await reader.ReadToEndAsync(cancellationToken);
    }

    private static async Task<string> ExtractPdfTextAsync(Stream content, CancellationToken cancellationToken)
    {
        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);
        using var document = PdfDocument.Open(buffer.ToArray());
        return string.Join("\n\n", document.GetPages().Select(page => page.Text));
    }
}
