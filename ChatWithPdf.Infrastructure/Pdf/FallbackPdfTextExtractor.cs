using System.Text.RegularExpressions;
using ChatWithPdf.Application.Contracts;
using ChatWithPdf.Application.Options;
using Google.GenAI;
using Google.GenAI.Types;
using Microsoft.Extensions.Options;
using UglyToad.PdfPig;

namespace ChatWithPdf.Infrastructure.Pdf;

public sealed partial class FallbackPdfTextExtractor(
    Client geminiClient,
    IOptions<RagOptions> options) : IPdfTextExtractor
{
    private static readonly Regex PageMarkerPattern = PageMarkerRegex();
    private readonly RagOptions _options = options.Value;

    public async Task<IReadOnlyList<PageText>> ExtractAsync(
        Stream pdfStream,
        CancellationToken cancellationToken = default)
    {
        var pdfBytes = await ReadAllBytesAsync(pdfStream, cancellationToken);

        var textPages = ExtractWithPdfPig(pdfBytes);
        if (textPages.Count > 0)
        {
            return textPages;
        }

        return await ExtractWithGeminiOcrAsync(pdfBytes, cancellationToken);
    }

    private static List<PageText> ExtractWithPdfPig(byte[] pdfBytes)
    {
        using var stream = new MemoryStream(pdfBytes);
        using var document = PdfDocument.Open(stream);

        return document.GetPages()
            .Select(page => new PageText(page.Number, page.Text.Trim()))
            .Where(page => !string.IsNullOrWhiteSpace(page.Text))
            .ToList();
    }

    private async Task<IReadOnlyList<PageText>> ExtractWithGeminiOcrAsync(
        byte[] pdfBytes,
        CancellationToken cancellationToken)
    {
        var response = await geminiClient.Models.GenerateContentAsync(
            model: _options.ChatModel,
            contents:
            [
                new Content
                {
                    Parts =
                    [
                        new Part
                        {
                            InlineData = new Blob
                            {
                                MimeType = "application/pdf",
                                Data = pdfBytes
                            }
                        },
                        new Part
                        {
                            Text =
                                """
                                Extract all readable text from this PDF, including handwritten and scanned content.
                                Return one section per page using this exact format:

                                ===PAGE 1===
                                text for page 1

                                ===PAGE 2===
                                text for page 2

                                Continue for every page in the document. If a page has no readable text, still include the page marker and leave the section empty.
                                """
                        }
                    ]
                }
            ],
            cancellationToken: cancellationToken);

        var extractedText = response.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;
        if (string.IsNullOrWhiteSpace(extractedText))
        {
            return [];
        }

        return ParsePageSections(extractedText);
    }

    private static List<PageText> ParsePageSections(string extractedText)
    {
        var pages = new List<PageText>();
        var matches = PageMarkerPattern.Matches(extractedText);

        foreach (Match match in matches)
        {
            if (!int.TryParse(match.Groups["page"].Value, out var pageNumber))
            {
                continue;
            }

            var text = match.Groups["text"].Value.Trim();
            if (!string.IsNullOrWhiteSpace(text))
            {
                pages.Add(new PageText(pageNumber, text));
            }
        }

        if (pages.Count == 0 && !string.IsNullOrWhiteSpace(extractedText))
        {
            pages.Add(new PageText(1, extractedText.Trim()));
        }

        return pages;
    }

    private static async Task<byte[]> ReadAllBytesAsync(Stream stream, CancellationToken cancellationToken)
    {
        if (stream is MemoryStream memoryStream && memoryStream.TryGetBuffer(out var buffer))
        {
            return buffer.AsSpan().ToArray();
        }

        using var copy = new MemoryStream();
        await stream.CopyToAsync(copy, cancellationToken);

        if (stream.CanSeek)
        {
            stream.Position = 0;
        }

        return copy.ToArray();
    }

    [GeneratedRegex(@"===PAGE\s+(?<page>\d+)===\s*(?<text>.*?)(?====PAGE\s+\d+===|\z)", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
    private static partial Regex PageMarkerRegex();
}
