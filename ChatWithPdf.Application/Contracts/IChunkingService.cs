namespace ChatWithPdf.Application.Contracts;

public interface IChunkingService
{
  IReadOnlyList<TextChunk> ChunkPage(int pageNumber, string text);
}

public sealed record TextChunk(int PageNumber, int ChunkIndex, string Content, int TokenEstimate);
