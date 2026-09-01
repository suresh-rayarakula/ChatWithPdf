namespace ChatWithPdf.Application.Contracts;

public interface IPdfTextExtractor
{
  Task<IReadOnlyList<PageText>> ExtractAsync(Stream pdfStream, CancellationToken cancellationToken = default);
}

public sealed record PageText(int PageNumber, string Text);
