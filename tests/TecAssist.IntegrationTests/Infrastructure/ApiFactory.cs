using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TecAssist.Application.Abstractions;
using TecAssist.Application.Options;
using TecAssist.Infrastructure.Nlp;
using TecAssist.Infrastructure.Persistence;
using TecAssist.IntegrationTests.Fakes;
using Testcontainers.PostgreSql;

namespace TecAssist.IntegrationTests.Infrastructure;

public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string DevUserId = "00000000-0000-0000-0000-00000000beef";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("pgvector/pgvector:pg16")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.UseSetting("ConnectionStrings:Default", _postgres.GetConnectionString());
        builder.UseSetting("Auth:Disabled", "true");
        builder.UseSetting("Auth:DevUserId", DevUserId);
        builder.UseSetting("Nvidia:ApiKey", "unused-in-tests");
        builder.UseSetting("Nvidia:ChatModel", "unused-in-tests");
        builder.UseSetting("Nvidia:EmbeddingModel", "unused-in-tests");
        builder.UseSetting("Rag:ChatStreamEmptyRetries", "1");
        builder.UseSetting("Rag:ChatStreamRetryDelay", "00:00:00.050");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IEmbeddingClient>();
            services.RemoveAll<IChatClient>();
            services.AddSingleton<IEmbeddingClient>(new FakeEmbeddingClient(dimensions: 2048));
            services.AddSingleton(new FakeStreamingChatClient("The maintenance interval", " is 90 days", " [1]."));
            services.AddSingleton<IChatClient>(provider => new ResilientChatClient(
                provider.GetRequiredService<FakeStreamingChatClient>(),
                provider.GetRequiredService<ILogger<ResilientChatClient>>(),
                provider.GetRequiredService<IOptions<RagOptions>>()));
        });
    }

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        using var scope = Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<TecAssistDbContext>();
        await database.Database.MigrateAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync();
        await _postgres.DisposeAsync();
    }
}
