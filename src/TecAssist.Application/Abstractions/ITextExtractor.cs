using TecAssist.Domain.Documents;

namespace TecAssist.Application.Abstractions;

public interface ITextExtractor
{
    Task<string> ExtractAsync(SourceType sourceType, Stream content, CancellationToken cancellationToken = default);
}
