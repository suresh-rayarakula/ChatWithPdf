namespace ChatWithPdf.Domain.Entities;

public class Document
{
    public Guid Id { get; set; }
    public required string FileName { get; set; }
    public string? ContentType { get; set; }
    public long FileSizeBytes { get; set; }
    public int PageCount { get; set; }
    public DateTime UploadedAt { get; set; }

    public ICollection<DocumentChunk> Chunks { get; set; } = [];
}
