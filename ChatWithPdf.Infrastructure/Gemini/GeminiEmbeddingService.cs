using ChatWithPdf.Application.Contracts;
using ChatWithPdf.Application.Options;
using Google.GenAI;
using Google.GenAI.Types;
using Microsoft.Extensions.Options;

namespace ChatWithPdf.Infrastructure.Gemini;

public sealed class GeminiEmbeddingService(
    Client geminiClient,
    IOptions<RagOptions> options) : IEmbeddingService
{
    private readonly Client _geminiClient = geminiClient;
    private readonly RagOptions _options = options.Value;

    public Task<float[]> EmbedAsync(string text, CancellationToken cancellationToken = default) =>
        EmbedInternalAsync(text, "RETRIEVAL_QUERY", cancellationToken);

    public async Task<IReadOnlyList<float[]>> EmbedBatchAsync(
        IReadOnlyList<string> texts,
        CancellationToken cancellationToken = default)
    {
        if (texts.Count == 0)
        {
            return [];
        }

        var embeddings = new float[texts.Count][];
        for (var i = 0; i < texts.Count; i++)
        {
            embeddings[i] = await EmbedInternalAsync(texts[i], "RETRIEVAL_DOCUMENT", cancellationToken);
        }

        return embeddings;
    }

    private async Task<float[]> EmbedInternalAsync(
        string text,
        string taskType,
        CancellationToken cancellationToken)
    {
        var config = new EmbedContentConfig
        {
            OutputDimensionality = _options.EmbeddingDimensions,
            TaskType = taskType
        };

        var response = await _geminiClient.Models.EmbedContentAsync(
            model: _options.EmbeddingModel,
            contents: text,
            config: config,
            cancellationToken: cancellationToken);

        var values = response.Embeddings?.FirstOrDefault()?.Values
            ?? throw new InvalidOperationException("Gemini returned no embedding values.");

        return values.Select(v => (float)v).ToArray();
    }
}
