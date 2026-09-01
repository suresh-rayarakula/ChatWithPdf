namespace ChatWithPdf.Application.Contracts;

public interface IVectorSearchService
{
  Task<IReadOnlyList<RetrievedChunk>> SearchAsync(
    Guid? documentId,
    float[] queryEmbedding,
    int topK,
    CancellationToken cancellationToken = default);
}

public sealed record RetrievedChunk(
  Guid ChunkId,
  Guid DocumentId,
  string FileName,
  int PageNumber,
  int ChunkIndex,
  string Content,
  double Similarity);
