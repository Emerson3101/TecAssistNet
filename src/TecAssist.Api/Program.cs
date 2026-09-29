using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Serilog;
using TecAssist.Api.Auth;
using TecAssist.Api.Middleware;
using TecAssist.Api.Services;
using TecAssist.Application;
using TecAssist.Application.Common;
using TecAssist.Infrastructure;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting TecAssist.API host");

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    builder.Services.AddControllers();
    builder.Services.AddOpenApi();

    builder.Services.AddApplication(builder.Configuration);
    builder.Services.AddInfrastructure(builder.Configuration);

    var authDisabled = builder.Configuration.GetValue("Auth:Disabled", false);
    if (authDisabled)
    {
        var devUserId = builder.Configuration.GetValue("Auth:DevUserId", "00000000-0000-0000-0000-000000000001");
        builder.Services.AddSingleton<ICurrentUser>(new DevCurrentUser(Guid.Parse(devUserId)));
    }
    else
    {
        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();
        builder.Services.AddSingleton<IConfigureOptions<JwtBearerOptions>, ConfigureJwtBearerOptions>();

        builder.Services.AddHttpContextAccessor();
        builder.Services.TryAddScoped<ICurrentUser, HttpContextCurrentUser>();
    }

    var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
    builder.Services.AddCors(options => options.AddPolicy("Web", policy => policy
        .WithOrigins(allowedOrigins)
        .WithHeaders("Authorization", "Content-Type")
        .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS")));

    var connectionString = builder.Configuration.GetConnectionString("Default");
    var postgresConnectionString = string.IsNullOrWhiteSpace(connectionString)
        ? "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=postgres"
        : connectionString;

    builder.Services.AddHealthChecks()
        .AddNpgSql(postgresConnectionString, name: "postgres", tags: ["ready"]);

    var app = builder.Build();

    app.UseSerilogRequestLogging();
    app.UseMiddleware<ExceptionHandlingMiddleware>();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
    }

    app.UseCors("Web");
    app.UseHttpsRedirection();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();

    app.MapHealthChecks("/health", new HealthCheckOptions
    {
        Predicate = _ => false
    });
    app.MapHealthChecks("/health/ready", new HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("ready")
    });

    app.Run();
}
catch (HostAbortedException)
{
    throw;
}
catch (Exception exception)
{
    Log.Fatal(exception, "TecAssist.API host terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program { }
