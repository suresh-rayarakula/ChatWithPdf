using ChatWithPdf.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ChatWithPdf.Api.Extensions;

public static class WebApplicationExtensions
{
    public static async Task MigrateDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await dbContext.Database.MigrateAsync();
    }
}
