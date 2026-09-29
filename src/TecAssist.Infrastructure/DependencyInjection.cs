using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TecAssist.Application.Persistence;
using TecAssist.Infrastructure.Persistence;

namespace TecAssist.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default");

        services.AddDbContext<TecAssistDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql => npgsql.UseVector()));

        services.AddScoped<ITecAssistDbContext>(provider => provider.GetRequiredService<TecAssistDbContext>());

        return services;
    }
}
