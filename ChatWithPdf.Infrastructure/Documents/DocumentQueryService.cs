using ChatWithPdf.Application.Contracts;
using ChatWithPdf.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ChatWithPdf.Infrastructure.Documents;

public sealed class DocumentQueryService(AppDbContext dbContext) : IDocumentQueryService
{
    public async Task<IReadOnlyList<DocumentSummary>> ListAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Documents
            .AsNoTracking()
            .OrderByDescending(document => document.UploadedAt)
            .Select(document => new DocumentSummary(
                document.Id,
                document.FileName,
                document.PageCount,
                document.UploadedAt,
                document.Chunks.Count))
            .ToListAsync(cancellationToken);
    }
}
