using ChatWithPdf.Application.Contracts;
using ChatWithPdf.Application.Options;
using Microsoft.Extensions.Options;
using OpenAI.Embeddings;

namespace ChatWithPdf.Infrastructure.OpenAI;

public sealed class OpenAIEmbeddingService(
    EmbeddingClient embeddingClient,
    IOptions<RagOptions> options) : IEmbeddingService
{
    private readonly RagOptions _options = options.Value;

    public async Task<float[]> EmbedAsync(string text, CancellationToken cancellationToken = default)
    {
        var result = await embeddingClient.GenerateEmbeddingAsync(
            text,
            new EmbeddingGenerationOptions
            {
                Dimensions = _options.EmbeddingDimensions
            },
            cancellationToken);

        return result.Value.ToFloats().ToArray();
    }

    public async Task<IReadOnlyList<float[]>> EmbedBatchAsync(
        IReadOnlyList<string> texts,
        CancellationToken cancellationToken = default)
    {
        if (texts.Count == 0)
        {
            return [];
        }

        var result = await embeddingClient.GenerateEmbeddingsAsync(
            texts,
            new EmbeddingGenerationOptions
            {
                Dimensions = _options.EmbeddingDimensions
            },
            cancellationToken);

        return result.Value
            .Select(embedding => embedding.ToFloats().ToArray())
            .ToArray();
    }
}
