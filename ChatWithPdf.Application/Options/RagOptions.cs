namespace ChatWithPdf.Application.Options;

public class RagOptions
{
    public const string SectionName = "Rag";

    public int ChunkSize { get; set; } = 800;
    public int ChunkOverlap { get; set; } = 150;
    public int TopK { get; set; } = 5;
    public string EmbeddingModel { get; set; } = "gemini-embedding-001";
    public string ChatModel { get; set; } = "gemini-3.6-flash";
    public int EmbeddingDimensions { get; set; } = 768;
}
