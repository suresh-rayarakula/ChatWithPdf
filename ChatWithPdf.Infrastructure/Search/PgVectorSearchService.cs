using ChatWithPdf.Application.Contracts;
using ChatWithPdf.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;

namespace ChatWithPdf.Infrastructure.Search;

public sealed class PgVectorSearchService(AppDbContext dbContext) : IVectorSearchService
{
    public async Task<IReadOnlyList<RetrievedChunk>> SearchAsync(
        Guid? documentId,
        float[] queryEmbedding,
        int topK,
        CancellationToken cancellationToken = default)
    {
        var queryVector = new Vector(queryEmbedding);

        var query = dbContext.Chunks
            .AsNoTracking()
            .Where(chunk => chunk.Embedding != null);

        if (documentId.HasValue)
        {
            query = query.Where(chunk => chunk.DocumentId == documentId.Value);
        }

        var results = await query
            .Select(chunk => new
            {
                chunk.Id,
                chunk.DocumentId,
                chunk.Document.FileName,
                chunk.PageNumber,
                chunk.ChunkIndex,
                chunk.Content,
                Distance = chunk.Embedding!.CosineDistance(queryVector)
            })
            .OrderBy(result => result.Distance)
            .Take(topK)
            .ToListAsync(cancellationToken);

        return results
            .Select(result => new RetrievedChunk(
                result.Id,
                result.DocumentId,
                result.FileName,
                result.PageNumber,
                result.ChunkIndex,
                result.Content,
                1 - result.Distance))
            .ToList();
    }
}
