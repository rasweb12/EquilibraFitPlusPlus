using System.Threading.RateLimiting;
using EquilibraFitPlusPlus.Api.Middleware;
using EquilibraFitPlusPlus.Application;
using EquilibraFitPlusPlus.Infrastructure;
using EquilibraFitPlusPlus.Infrastructure.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using EquilibraFitPlusPlus.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.OpenApi;
using Serilog;
using Serilog.Context;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(new Serilog.Formatting.Json.JsonFormatter())
    .CreateBootstrapLogger();

try
{
    WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) =>
    {
        configuration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .WriteTo.Console(new Serilog.Formatting.Json.JsonFormatter());
    });

    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddControllers();
    string[] allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
    if (builder.Environment.IsDevelopment() && allowedOrigins.Length == 0)
    {
        allowedOrigins = ["http://localhost:8080", "http://127.0.0.1:8080"];
    }
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("ApiClients", policy =>
        {
            if (allowedOrigins.Length > 0) policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod();
        });
    });

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "EquilibraFit++ API",
            Version = "v1",
            Description = "Sua saúde. Seu ritmo. Seu equilíbrio."
        });

        var securityScheme = new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Description = "Informe o token JWT no formato: Bearer {token}",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.Http,
            Scheme = JwtBearerDefaults.AuthenticationScheme,
            BearerFormat = "JWT"
        };

        options.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, securityScheme);
        options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference(JwtBearerDefaults.AuthenticationScheme, document)] = []
        });
    });

    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("AdminOnly", policy => policy.RequireRole("Administrador"));
    });

    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.AddFixedWindowLimiter("auth", limiter =>
        {
            limiter.PermitLimit = 10;
            limiter.Window = TimeSpan.FromMinutes(1);
            limiter.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            limiter.QueueLimit = 0;
        });
    });

    builder.Services.AddHealthChecks().AddCheck<DatabaseReadyHealthCheck>("database", tags: ["ready"]);

    WebApplication app = builder.Build();

    string provider = builder.Configuration["DATABASE_PROVIDER"] ?? builder.Configuration["Database:Provider"] ?? "PostgreSQL";
    if (provider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase))
    {
        if (!app.Environment.IsDevelopment() && !app.Environment.IsEnvironment("Testing"))
            throw new InvalidOperationException("SQLite is limited to Development and Testing. Production requires PostgreSQL.");
        using var scope = app.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<EquilibraFitPlusPlusDbContext>().Database.EnsureCreatedAsync();
    }

    app.UseMiddleware<ExceptionHandlingMiddleware>();

    app.Use(async (context, next) =>
    {
        string incoming = context.Request.Headers["X-Correlation-ID"].ToString();
        string correlation = Guid.TryParse(incoming, out var id) ? id.ToString("D") : Guid.NewGuid().ToString("D");
        context.Items["CorrelationId"] = correlation;
        context.Response.Headers["X-Correlation-ID"] = correlation;
        context.Response.Headers["X-Request-ID"] = context.TraceIdentifier;
        using (LogContext.PushProperty("CorrelationId", correlation))
        using (LogContext.PushProperty("RequestId", context.TraceIdentifier))
            await next();
    });
    app.UseSerilogRequestLogging();

    app.Use(async (context, next) =>
    {
        context.Response.Headers.TryAdd("X-Content-Type-Options", "nosniff");
        context.Response.Headers.TryAdd("X-Frame-Options", "DENY");
        context.Response.Headers.TryAdd("Referrer-Policy", "no-referrer");
        context.Response.Headers.TryAdd("X-XSS-Protection", "0");
        await next();
    });

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json", "EquilibraFit++ API v1"));
    }

    if (!app.Environment.IsDevelopment() && !app.Environment.IsEnvironment("Testing"))
    {
        app.UseHttpsRedirection();
    }

    app.UseRateLimiter();
    app.UseCors("ApiClients");

    app.UseAuthentication();
    app.UseMiddleware<IdempotencyMiddleware>();
    app.UseAuthorization();

    app.MapHealthChecks("/health", new HealthCheckOptions { Predicate = _ => false });
    app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });
    app.MapControllers();

    app.Run();
}
catch (Exception exception)
{
    Log.Fatal(exception, "EquilibraFit++ API failed to start.");
    Environment.ExitCode = 1;
}
finally
{
    Log.CloseAndFlush();
}

/// <summary>
/// API entry point marker used by integration tests.
/// </summary>
public partial class Program;
