using ChatWithPdf.Application.Options;
using ChatWithPdf.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Pgvector.EntityFrameworkCore;

namespace ChatWithPdf.Infrastructure.Persistence;

public class AppDbContext(
    DbContextOptions<AppDbContext> options,
    IOptions<RagOptions> ragOptions) : DbContext(options)
{
    private readonly RagOptions _ragOptions = ragOptions.Value;
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<DocumentChunk> Chunks => Set<DocumentChunk>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("vector");

        modelBuilder.Entity<Document>(entity =>
        {
            entity.HasKey(d => d.Id);
            entity.Property(d => d.FileName).HasMaxLength(500).IsRequired();
            entity.Property(d => d.ContentType).HasMaxLength(200);
            entity.HasIndex(d => d.UploadedAt);
        });

        modelBuilder.Entity<DocumentChunk>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Content).IsRequired();
            entity.Property(c => c.Embedding).HasColumnType($"vector({_ragOptions.EmbeddingDimensions})");
            entity.HasOne(c => c.Document)
                .WithMany(d => d.Chunks)
                .HasForeignKey(c => c.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(c => new { c.DocumentId, c.PageNumber, c.ChunkIndex }).IsUnique();
            entity.HasIndex(c => c.Embedding)
                .HasMethod("hnsw")
                .HasOperators("vector_cosine_ops");
        });
    }
}
