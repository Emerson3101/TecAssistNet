using System.Net.Http.Headers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TecAssist.Application.Abstractions;
using TecAssist.Application.Persistence;
using TecAssist.Application.Rag;
using TecAssist.Infrastructure.Nlp;
using TecAssist.Infrastructure.Persistence;
using TecAssist.Infrastructure.Search;
using TecAssist.Infrastructure.Text;
using TecAssist.Infrastructure.Workers;

namespace TecAssist.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default");

        services.AddDbContext<TecAssistDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql => npgsql.UseVector())
                .UseSnakeCaseNamingConvention());

        services.AddScoped<ITecAssistDbContext>(provider => provider.GetRequiredService<TecAssistDbContext>());

        services.Configure<NvidiaOptions>(configuration.GetSection(NvidiaOptions.SectionName));
        services.AddHttpClient<IEmbeddingClient, NvidiaEmbeddingClient>((provider, httpClient) =>
        {
            var settings = provider.GetRequiredService<IOptions<NvidiaOptions>>().Value;
            httpClient.BaseAddress = new Uri(settings.BaseUrl.TrimEnd('/') + "/");
            httpClient.Timeout = TimeSpan.FromMinutes(5);
            httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", settings.ApiKey);
        });
        services.AddSingleton<IChatClient, NvidiaChatClient>();
        services.AddScoped<ITextExtractor, TextExtractor>();
        services.AddScoped<IChunkSearcher, PgVectorChunkSearcher>();
        services.AddHostedService<IngestionWorker>();

        return services;
    }
}
