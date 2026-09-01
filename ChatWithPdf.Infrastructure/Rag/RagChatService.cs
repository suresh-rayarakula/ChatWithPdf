using System.Text;
using ChatWithPdf.Application.Contracts;
using ChatWithPdf.Application.Options;
using Microsoft.Extensions.Options;
using OpenAI.Chat;

namespace ChatWithPdf.Infrastructure.Rag;

public sealed class RagChatService(
    IEmbeddingService embeddingService,
    IVectorSearchService vectorSearchService,
    ChatClient chatClient,
    IOptions<RagOptions> options) : IRagChatService
{
    private readonly RagOptions _options = options.Value;

    public async Task<ChatAnswer> AskAsync(ChatQuestion question, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(question.Question))
        {
            throw new ArgumentException("Question is required.", nameof(question));
        }

        var queryEmbedding = await embeddingService.EmbedAsync(question.Question, cancellationToken);
        var retrievedChunks = await vectorSearchService.SearchAsync(
            question.DocumentId,
            queryEmbedding,
            _options.TopK,
            cancellationToken);

        if (retrievedChunks.Count == 0)
        {
            return new ChatAnswer(
                "I could not find any indexed content to answer that question. Upload a PDF first.",
                []);
        }

        var context = BuildContextBlock(retrievedChunks);
        var messages = new List<ChatMessage>
        {
            new SystemChatMessage(
                """
                You are a helpful assistant that answers questions using only the provided context.
                If the answer is not in the context, say you do not know based on the uploaded documents.
                Keep answers concise and grounded in the source material.
                When useful, mention the page number from the source.
                """),
            new UserChatMessage(
                $"""
                Context:
                {context}

                Question:
                {question.Question}
                """)
        };

        var completion = await chatClient.CompleteChatAsync(messages, cancellationToken: cancellationToken);
        var answer = completion.Value.Content.FirstOrDefault()?.Text
            ?? "I could not generate an answer.";

        var sources = retrievedChunks
            .Select(chunk => new SourceCitation(
                chunk.DocumentId,
                chunk.FileName,
                chunk.PageNumber,
                chunk.ChunkIndex,
                Truncate(chunk.Content, 240),
                chunk.Similarity))
            .ToList();

        return new ChatAnswer(answer, sources);
    }

    private static string BuildContextBlock(IReadOnlyList<RetrievedChunk> chunks)
    {
        var builder = new StringBuilder();

        for (var i = 0; i < chunks.Count; i++)
        {
            var chunk = chunks[i];
            builder.AppendLine($"[Source {i + 1} | {chunk.FileName} | page {chunk.PageNumber}]");
            builder.AppendLine(chunk.Content);
            builder.AppendLine();
        }

        return builder.ToString();
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : $"{value[..maxLength]}...";
}
