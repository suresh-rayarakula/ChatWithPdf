using ChatWithPdf.Application.Contracts;
using UglyToad.PdfPig;

namespace ChatWithPdf.Infrastructure.Pdf;

public sealed class PdfPigTextExtractor : IPdfTextExtractor
{
    public Task<IReadOnlyList<PageText>> ExtractAsync(Stream pdfStream, CancellationToken cancellationToken = default)
    {
        using var document = PdfDocument.Open(pdfStream);

        var pages = document.GetPages()
            .Select(page => new PageText(page.Number, page.Text.Trim()))
            .Where(page => !string.IsNullOrWhiteSpace(page.Text))
            .ToList();

        return Task.FromResult<IReadOnlyList<PageText>>(pages);
    }
}
