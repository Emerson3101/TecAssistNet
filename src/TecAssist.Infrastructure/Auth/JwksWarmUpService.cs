using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace TecAssist.Infrastructure.Auth;

public sealed class JwksWarmUpService(
    JwksProvider jwksProvider,
    IOptions<SupabaseOptions> options,
    ILogger<JwksWarmUpService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.Url))
        {
            return;
        }

        try
        {
            await jwksProvider.GetSigningKeysAsync(settings.ResolveJwksUrl(), cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Could not warm the Supabase JWKS cache at startup");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
