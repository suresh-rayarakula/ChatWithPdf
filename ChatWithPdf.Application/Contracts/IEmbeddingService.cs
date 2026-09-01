namespace ChatWithPdf.Application.Contracts;

public interface IEmbeddingService
{
  Task<float[]> EmbedAsync(string text, CancellationToken cancellationToken = default);
  Task<IReadOnlyList<float[]>> EmbedBatchAsync(IReadOnlyList<string> texts, CancellationToken cancellationToken = default);
}
