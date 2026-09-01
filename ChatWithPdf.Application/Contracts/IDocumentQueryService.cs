namespace ChatWithPdf.Application.Contracts;

public interface IDocumentQueryService
{
    Task<IReadOnlyList<DocumentSummary>> ListAsync(CancellationToken cancellationToken = default);
}

public sealed record DocumentSummary(
    Guid Id,
    string FileName,
    int PageCount,
    DateTime UploadedAt,
    int ChunkCount);
