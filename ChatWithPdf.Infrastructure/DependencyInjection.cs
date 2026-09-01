using ChatWithPdf.Application.Contracts;
using ChatWithPdf.Application.Options;
using ChatWithPdf.Infrastructure.Chunking;
using ChatWithPdf.Infrastructure.Documents;
using ChatWithPdf.Infrastructure.Ingestion;
using ChatWithPdf.Infrastructure.OpenAI;
using ChatWithPdf.Infrastructure.Pdf;
using ChatWithPdf.Infrastructure.Persistence;
using ChatWithPdf.Infrastructure.Rag;
using ChatWithPdf.Infrastructure.Search;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenAI;
using OpenAI.Chat;
using OpenAI.Embeddings;
using Pgvector.EntityFrameworkCore;

namespace ChatWithPdf.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<RagOptions>(configuration.GetSection(RagOptions.SectionName));

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is missing.");

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql => npgsql.UseVector()));

        var openAiApiKey = configuration["OpenAI:ApiKey"]
            ?? throw new InvalidOperationException("OpenAI:ApiKey is missing.");

        var ragOptions = configuration.GetSection(RagOptions.SectionName).Get<RagOptions>() ?? new RagOptions();
        var openAiClient = new OpenAIClient(openAiApiKey);

        services.AddSingleton(openAiClient.GetEmbeddingClient(ragOptions.EmbeddingModel));
        services.AddSingleton(openAiClient.GetChatClient(ragOptions.ChatModel));

        services.AddScoped<IPdfTextExtractor, PdfPigTextExtractor>();
        services.AddScoped<IChunkingService, SlidingWindowChunkingService>();
        services.AddScoped<IEmbeddingService, OpenAIEmbeddingService>();
        services.AddScoped<IVectorSearchService, PgVectorSearchService>();
        services.AddScoped<IDocumentIngestionService, DocumentIngestionService>();
        services.AddScoped<IDocumentQueryService, DocumentQueryService>();
        services.AddScoped<IRagChatService, RagChatService>();

        return services;
    }
}
