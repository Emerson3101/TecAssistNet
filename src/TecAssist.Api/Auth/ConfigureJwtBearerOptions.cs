using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TecAssist.Infrastructure.Auth;

namespace TecAssist.Api.Auth;

public sealed class ConfigureJwtBearerOptions(
    JwksProvider jwksProvider,
    IOptions<SupabaseOptions> supabaseOptions) : IConfigureOptions<JwtBearerOptions>
{
    public void Configure(JwtBearerOptions options)
    {
        var supabase = supabaseOptions.Value;
        var algorithm = string.IsNullOrWhiteSpace(supabase.JwtSecret) ? "ES256" : "HS256";

        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = supabase.ResolveIssuer(),
            ValidateAudience = true,
            ValidAudience = "authenticated",
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
            ValidAlgorithms = [algorithm]
        };

        if (!string.IsNullOrWhiteSpace(supabase.JwtSecret))
        {
            options.TokenValidationParameters.IssuerSigningKey =
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(supabase.JwtSecret));
        }
        else
        {
            options.TokenValidationParameters.IssuerSigningKeyResolver = (_, _, _, _) =>
                jwksProvider.GetSigningKeys(supabase.ResolveJwksUrl());
        }
    }
}
