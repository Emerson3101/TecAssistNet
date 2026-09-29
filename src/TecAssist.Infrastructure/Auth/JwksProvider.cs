using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace TecAssist.Infrastructure.Auth;

public sealed class JwksProvider(HttpClient httpClient, IMemoryCache cache, ILogger<JwksProvider> logger)
{
    private const string CacheKey = "supabase-signing-keys";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);
    private readonly SemaphoreSlim _refreshGate = new(1, 1);

    public IReadOnlyList<SecurityKey> GetSigningKeys(string jwksUrl) =>
        GetSigningKeysAsync(jwksUrl, CancellationToken.None).GetAwaiter().GetResult();

    public async Task<IReadOnlyList<SecurityKey>> GetSigningKeysAsync(string jwksUrl, CancellationToken cancellationToken)
    {
        if (cache.TryGetValue<IReadOnlyList<SecurityKey>>(CacheKey, out var cached) && cached is not null)
        {
            return cached;
        }

        await _refreshGate.WaitAsync(cancellationToken);
        try
        {
            if (cache.TryGetValue<IReadOnlyList<SecurityKey>>(CacheKey, out cached) && cached is not null)
            {
                return cached;
            }

            var json = await httpClient.GetStringAsync(jwksUrl, cancellationToken);
            var signingKeys = new JsonWebKeySet(json).GetSigningKeys();
            if (signingKeys.Count == 0)
            {
                throw new InvalidOperationException("The Supabase JWKS endpoint returned no signing keys.");
            }

            cache.Set(CacheKey, (IReadOnlyList<SecurityKey>)[.. signingKeys], CacheDuration);
            logger.LogInformation("Refreshed {Count} Supabase JWT signing key(s)", signingKeys.Count);
            return [.. signingKeys];
        }
        finally
        {
            _refreshGate.Release();
        }
    }
}
