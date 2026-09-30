using System.Net;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using TecAssist.Infrastructure.Auth;

namespace TecAssist.UnitTests.Auth;

public sealed class JwksProviderTests
{
    private const string JwksJson =
        """
        {"keys":[{"alg":"ES256","crv":"P-256","ext":true,"key_ops":["verify"],"kid":"a2e840dd-23d0-4dbe-948a-c9aba2d90a4e","kty":"EC","use":"sig","x":"fdc5FZMs7lVw5H0PjM8lZw0ceP61c3X-LQEBG6UoSJ0","y":"UU6EF8rfsa0PMUW3heiugQyEjOwrJe5hfy70fLzWxWc"}]}
        """;

    private const string JwksUrl = "https://test.supabase.co/auth/v1/.well-known/jwks.json";

    private sealed class CountingHandler : HttpMessageHandler
    {
        public int RequestCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestCount++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JwksJson),
            });
        }
    }

    private static JwksProvider CreateProvider(HttpMessageHandler handler)
    {
        return new JwksProvider(
            new HttpClient(handler),
            new MemoryCache(new MemoryCacheOptions()),
            NullLogger<JwksProvider>.Instance);
    }

    [Fact]
    public async Task GetSigningKeys_ParsesEs256Key_FromJwks()
    {
        var handler = new CountingHandler();
        var provider = CreateProvider(handler);

        var keys = await provider.GetSigningKeysAsync(JwksUrl, CancellationToken.None);

        var key = Assert.Single(keys);
        var ecKey = Assert.IsType<Microsoft.IdentityModel.Tokens.ECDsaSecurityKey>(key);
        Assert.NotNull(ecKey.ECDsa);
    }

    [Fact]
    public async Task GetSigningKeys_CachesKeys_AndAvoidsRepeatRequests()
    {
        var handler = new CountingHandler();
        var provider = CreateProvider(handler);

        await provider.GetSigningKeysAsync(JwksUrl, CancellationToken.None);
        await provider.GetSigningKeysAsync(JwksUrl, CancellationToken.None);
        await provider.GetSigningKeysAsync(JwksUrl, CancellationToken.None);

        Assert.Equal(1, handler.RequestCount);
    }

    [Fact]
    public async Task GetSigningKeys_Throws_WhenEndpointReturnsNoKeys()
    {
        var emptyHandler = new EmptyJwksHandler();
        var provider = CreateProvider(emptyHandler);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => provider.GetSigningKeysAsync(JwksUrl, CancellationToken.None));
    }

    private sealed class EmptyJwksHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"keys":[]}"""),
            });
        }
    }
}
