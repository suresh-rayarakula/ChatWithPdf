namespace ChatWithPdf.Application.Contracts;

public interface IDocumentIngestionService
{
  Task<DocumentIngestionResult> IngestAsync(
    string fileName,
    string? contentType,
    Stream pdfStream,
    CancellationToken cancellationToken = default);
}

public sealed record DocumentIngestionResult(
  Guid DocumentId,
  string FileName,
  int PageCount,
  int ChunkCount);
