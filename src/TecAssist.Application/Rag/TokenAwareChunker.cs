using Microsoft.Extensions.Options;
using TecAssist.Application.Abstractions;
using TecAssist.Application.Options;

namespace TecAssist.Application.Rag;

public sealed class TokenAwareChunker(IOptions<RagOptions> options) : IChunker
{
    private static readonly char[] WordSeparators = [' ', '\t'];

    public IReadOnlyList<TextChunk> Chunk(string text)
    {
        var settings = options.Value;
        var maxWords = Math.Max(1, settings.ChunkSizeTokens);
        var overlapWords = Math.Clamp(settings.OverlapTokens, 0, maxWords / 2);

        var chunks = new List<TextChunk>();
        var paragraphBuffer = new List<string>();
        var bufferedWords = 0;

        foreach (var paragraph in SplitParagraphs(text))
        {
            var words = paragraph.Split(WordSeparators, StringSplitOptions.RemoveEmptyEntries);
            if (words.Length == 0)
            {
                continue;
            }

            if (words.Length <= maxWords)
            {
                if (bufferedWords > 0 && bufferedWords + words.Length > maxWords)
                {
                    FlushBuffer();
                }

                paragraphBuffer.Add(paragraph);
                bufferedWords += words.Length;
            }
            else
            {
                FlushBuffer();

                var offset = 0;
                while (offset < words.Length)
                {
                    var size = Math.Min(maxWords, words.Length - offset);
                    chunks.Add(new TextChunk(string.Join(' ', words.Skip(offset).Take(size)), size));
                    if (offset + size >= words.Length)
                    {
                        break;
                    }

                    offset += size - overlapWords;
                }
            }
        }

        FlushBuffer();
        return chunks;

        void FlushBuffer()
        {
            if (paragraphBuffer.Count == 0)
            {
                return;
            }

            chunks.Add(new TextChunk(string.Join("\n\n", paragraphBuffer), bufferedWords));
            paragraphBuffer.Clear();
            bufferedWords = 0;
        }
    }

    private static IEnumerable<string> SplitParagraphs(string text)
    {
        return text.Replace("\r\n", "\n")
            .Split("\n\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }
}
