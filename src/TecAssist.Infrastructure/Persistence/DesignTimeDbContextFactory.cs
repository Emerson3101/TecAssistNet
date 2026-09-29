using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TecAssist.Infrastructure.Persistence;

public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<TecAssistDbContext>
{
    public TecAssistDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<TecAssistDbContext>()
            .UseNpgsql(
                "Host=localhost;Database=tecasist;Username=postgres;Password=postgres",
                npgsql => npgsql.UseVector())
            .UseSnakeCaseNamingConvention()
            .Options;

        return new TecAssistDbContext(options);
    }
}
