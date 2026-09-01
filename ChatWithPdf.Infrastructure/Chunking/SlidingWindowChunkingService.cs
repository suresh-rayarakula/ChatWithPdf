using ChatWithPdf.Application.Contracts;
using ChatWithPdf.Application.Options;
using Microsoft.Extensions.Options;

namespace ChatWithPdf.Infrastructure.Chunking;

public sealed class SlidingWindowChunkingService(IOptions<RagOptions> options) : IChunkingService
{
    private readonly RagOptions _options = options.Value;

    public IReadOnlyList<TextChunk> ChunkPage(int pageNumber, string text)
    {
        var normalized = NormalizeWhitespace(text);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return [];
        }

        var chunks = new List<TextChunk>();
        var start = 0;
        var chunkIndex = 0;

        while (start < normalized.Length)
        {
            var end = Math.Min(start + _options.ChunkSize, normalized.Length);

            if (end < normalized.Length)
            {
                var splitAt = FindSplitPoint(normalized, start, end);
                if (splitAt > start)
                {
                    end = splitAt;
                }
            }

            var content = normalized[start..end].Trim();
            if (content.Length > 0)
            {
                chunks.Add(new TextChunk(pageNumber, chunkIndex++, content, EstimateTokens(content)));
            }

            if (end >= normalized.Length)
            {
                break;
            }

            start = Math.Max(end - _options.ChunkOverlap, start + 1);
        }

        return chunks;
    }

    private int FindSplitPoint(string text, int start, int end)
    {
        var window = text[start..end];
        var lastParagraph = window.LastIndexOf(". ", StringComparison.Ordinal);
        if (lastParagraph > _options.ChunkSize / 3)
        {
            return start + lastParagraph + 1;
        }

        var lastSpace = window.LastIndexOf(' ');
        return lastSpace > _options.ChunkSize / 3 ? start + lastSpace : end;
    }

    private static string NormalizeWhitespace(string text) =>
        string.Join(' ', text.Split(['\r', '\n', '\t', ' '], StringSplitOptions.RemoveEmptyEntries));

    private static int EstimateTokens(string text) => Math.Max(1, text.Length / 4);
}
