using ChatWithPdf.Application.Contracts;
using ChatWithPdf.Application.Options;
using ChatWithPdf.Infrastructure.Chunking;
using ChatWithPdf.Infrastructure.Documents;
using ChatWithPdf.Infrastructure.Gemini;
using ChatWithPdf.Infrastructure.Ingestion;
using ChatWithPdf.Infrastructure.Pdf;
using ChatWithPdf.Infrastructure.Persistence;
using ChatWithPdf.Infrastructure.Rag;
using ChatWithPdf.Infrastructure.Search;
using Google.GenAI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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

        var geminiApiKey = configuration["Gemini:ApiKey"]
            ?? throw new InvalidOperationException("Gemini:ApiKey is missing.");

        services.AddSingleton(new Client(apiKey: geminiApiKey));

        services.AddScoped<IPdfTextExtractor, FallbackPdfTextExtractor>();
        services.AddScoped<IChunkingService, SlidingWindowChunkingService>();
        services.AddScoped<IEmbeddingService, GeminiEmbeddingService>();
        services.AddScoped<IVectorSearchService, PgVectorSearchService>();
        services.AddScoped<IDocumentIngestionService, DocumentIngestionService>();
        services.AddScoped<IDocumentQueryService, DocumentQueryService>();
        services.AddScoped<IRagChatService, RagChatService>();

        return services;
    }
}
