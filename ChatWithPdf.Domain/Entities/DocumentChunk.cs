using Pgvector;

namespace ChatWithPdf.Domain.Entities;

public class DocumentChunk
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public int PageNumber { get; set; }
    public int ChunkIndex { get; set; }
    public required string Content { get; set; }
    public Vector? Embedding { get; set; }
    public int TokenEstimate { get; set; }

    public Document Document { get; set; } = null!;
}
