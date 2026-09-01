namespace ChatWithPdf.Application.Options;

public class RagOptions
{
    public const string SectionName = "Rag";

    public int ChunkSize { get; set; } = 800;
    public int ChunkOverlap { get; set; } = 150;
    public int TopK { get; set; } = 5;
    public string EmbeddingModel { get; set; } = "text-embedding-3-small";
    public string ChatModel { get; set; } = "gpt-4o-mini";
    public int EmbeddingDimensions { get; set; } = 1536;
}
