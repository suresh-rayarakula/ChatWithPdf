using ChatWithPdf.Application.Contracts;
using ChatWithPdf.Domain.Entities;
using ChatWithPdf.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Pgvector;

namespace ChatWithPdf.Infrastructure.Ingestion;

public sealed class DocumentIngestionService(
    AppDbContext dbContext,
    IPdfTextExtractor pdfTextExtractor,
    IChunkingService chunkingService,
    IEmbeddingService embeddingService) : IDocumentIngestionService
{
    public async Task<DocumentIngestionResult> IngestAsync(
        string fileName,
        string? contentType,
        Stream pdfStream,
        CancellationToken cancellationToken = default)
    {
        var pages = await pdfTextExtractor.ExtractAsync(pdfStream, cancellationToken);
        if (pages.Count == 0)
        {
            throw new InvalidOperationException("No extractable text was found in the PDF.");
        }

        var textChunks = pages
            .SelectMany(page => chunkingService.ChunkPage(page.PageNumber, page.Text))
            .ToList();

        if (textChunks.Count == 0)
        {
            throw new InvalidOperationException("Chunking produced no content from the PDF.");
        }

        var document = new Document
        {
            Id = Guid.NewGuid(),
            FileName = fileName,
            ContentType = contentType,
            FileSizeBytes = pdfStream.CanSeek ? pdfStream.Length : 0,
            PageCount = pages.Count,
            UploadedAt = DateTime.UtcNow
        };

        var embeddings = await embeddingService.EmbedBatchAsync(
            textChunks.Select(chunk => chunk.Content).ToList(),
            cancellationToken);

        var chunks = textChunks
            .Zip(embeddings, (chunk, embedding) => new DocumentChunk
            {
                Id = Guid.NewGuid(),
                DocumentId = document.Id,
                PageNumber = chunk.PageNumber,
                ChunkIndex = chunk.ChunkIndex,
                Content = chunk.Content,
                TokenEstimate = chunk.TokenEstimate,
                Embedding = new Vector(embedding)
            })
            .ToList();

        document.Chunks = chunks;

        dbContext.Documents.Add(document);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new DocumentIngestionResult(document.Id, document.FileName, document.PageCount, chunks.Count);
    }
}
