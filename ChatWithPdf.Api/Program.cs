using ChatWithPdf.Application.Contracts;
using ChatWithPdf.Infrastructure;
using ChatWithPdf.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapPost("/api/documents/upload", async (
    IFormFile file,
    IDocumentIngestionService ingestionService,
    CancellationToken cancellationToken) =>
{
    if (file.Length == 0)
    {
        return Results.BadRequest(new { error = "Upload a non-empty PDF file." });
    }

    if (!string.Equals(file.ContentType, "application/pdf", StringComparison.OrdinalIgnoreCase)
        && !file.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
    {
        return Results.BadRequest(new { error = "Only PDF files are supported." });
    }

    await using var stream = file.OpenReadStream();
    var result = await ingestionService.IngestAsync(file.FileName, file.ContentType, stream, cancellationToken);

    return Results.Ok(result);
})
.DisableAntiforgery();

app.MapGet("/api/documents", async (AppDbContext dbContext, CancellationToken cancellationToken) =>
{
    var documents = await dbContext.Documents
        .AsNoTracking()
        .OrderByDescending(document => document.UploadedAt)
        .Select(document => new
        {
            document.Id,
            document.FileName,
            document.PageCount,
            document.UploadedAt,
            ChunkCount = document.Chunks.Count
        })
        .ToListAsync(cancellationToken);

    return Results.Ok(documents);
});

app.MapPost("/api/chat", async (
    ChatRequest request,
    IRagChatService ragChatService,
    CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.Question))
    {
        return Results.BadRequest(new { error = "Question is required." });
    }

    var answer = await ragChatService.AskAsync(
        new ChatQuestion(request.Question, request.DocumentId),
        cancellationToken);

    return Results.Ok(answer);
});

app.Run();

public sealed record ChatRequest(string Question, Guid? DocumentId = null);
