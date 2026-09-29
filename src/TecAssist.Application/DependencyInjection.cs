using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TecAssist.Application.Abstractions;
using TecAssist.Application.Conversations;
using TecAssist.Application.Documents;
using TecAssist.Application.Options;
using TecAssist.Application.Queues;
using TecAssist.Application.Rag;

namespace TecAssist.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RagOptions>(configuration.GetSection(RagOptions.SectionName));

        services.AddSingleton<IChunker, TokenAwareChunker>();
        services.AddSingleton<IIngestionQueue, ChannelIngestionQueue>();
        services.AddSingleton<PromptBuilder>();

        services.AddScoped<IngestionPipeline>();
        services.AddScoped<DocumentService>();
        services.AddScoped<ConversationService>();
        services.AddScoped<ChatService>();

        return services;
    }
}
