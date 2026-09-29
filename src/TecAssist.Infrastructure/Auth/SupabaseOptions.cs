namespace TecAssist.Infrastructure.Auth;

public sealed class SupabaseOptions
{
    public const string SectionName = "Supabase";

    public string Url { get; set; } = string.Empty;

    public string? JwksUrl { get; set; }

    public string? JwtSecret { get; set; }

    public string ResolveJwksUrl() =>
        string.IsNullOrWhiteSpace(JwksUrl)
            ? Url.TrimEnd('/') + "/auth/v1/.well-known/jwks.json"
            : JwksUrl!;

    public string ResolveIssuer() => Url.TrimEnd('/') + "/auth/v1";
}
