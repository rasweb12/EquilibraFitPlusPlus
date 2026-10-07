using EquilibraFitPlusPlus.Application.Abstractions.Authentication;
using EquilibraFitPlusPlus.Application.Abstractions.Admin;
using EquilibraFitPlusPlus.Application.Abstractions.AiContext;
using EquilibraFitPlusPlus.Application.Abstractions.AiCoach;
using EquilibraFitPlusPlus.Application.Abstractions.Alimentacao;
using EquilibraFitPlusPlus.Application.Abstractions.Data;
using EquilibraFitPlusPlus.Application.Abstractions.Dashboard;
using EquilibraFitPlusPlus.Application.Abstractions.Evolucao;
using EquilibraFitPlusPlus.Application.Abstractions.Habitos;
using EquilibraFitPlusPlus.Application.Abstractions.Lgpd;
using EquilibraFitPlusPlus.Application.Abstractions.Marketplace;
using EquilibraFitPlusPlus.Application.Abstractions.Notificacoes;
using EquilibraFitPlusPlus.Application.Abstractions.Onboarding;
using EquilibraFitPlusPlus.Application.Abstractions.Planos;
using EquilibraFitPlusPlus.Application.Abstractions.Premium;
using EquilibraFitPlusPlus.Application.Abstractions.Relatorios;
using EquilibraFitPlusPlus.Application.Abstractions.Treinos;
using EquilibraFitPlusPlus.Application.Abstractions.Users;
using EquilibraFitPlusPlus.Infrastructure.Authentication;
using EquilibraFitPlusPlus.Infrastructure.AdminBootstrap;
using EquilibraFitPlusPlus.Infrastructure.AiContext;
using EquilibraFitPlusPlus.Infrastructure.AiCoach;
using EquilibraFitPlusPlus.Infrastructure.Data;
using EquilibraFitPlusPlus.Infrastructure.Repositories;
using EquilibraFitPlusPlus.Infrastructure.Billing;
using EquilibraFitPlusPlus.Application.Abstractions.Billing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace EquilibraFitPlusPlus.Infrastructure;

/// <summary>
/// Registers infrastructure services.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds infrastructure dependencies.
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        string connectionString = DatabaseConfiguration.ResolveConnectionString(
            configuration["SUPABASE_DB_CONNECTION_STRING"], configuration.GetConnectionString("Default"));

        services.Configure<AiCoachOptions>(configuration.GetSection(AiCoachOptions.SectionName));
        services.Configure<AdminBootstrapOptions>(configuration.GetSection(AdminBootstrapOptions.SectionName));
        services.AddSingleton(GooglePlayOptions.FromConfiguration(configuration));
        services.AddSingleton<IGooglePlayAccessTokenProvider, GooglePlayAccessTokenProvider>();
        services.AddSingleton<BillingTokenProtector>();
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<IBillingService, GooglePlayBillingService>();
        services.AddHostedService<GooglePlayReconciliationWorker>();
        services.AddHttpClient<IPaymentProvider, GooglePlayBillingProvider>().RemoveAllLoggers();
        services.AddHostedService<AdminBootstrapHostedService>();

        string databaseProvider = configuration["DATABASE_PROVIDER"] ?? configuration["Database:Provider"] ?? "PostgreSQL";
        services.AddDbContext<EquilibraFitPlusPlusDbContext>(options =>
            DatabaseConfiguration.Configure(options, databaseProvider, connectionString));

        string? redisConnection = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrWhiteSpace(redisConnection))
        {
            services.AddStackExchangeRedisCache(options => options.Configuration = redisConnection);
        }
        else
        {
            services.AddDistributedMemoryCache();
        }

        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<EquilibraFitPlusPlusDbContext>());
        services.AddSupabaseAuthentication(configuration);
        services.AddHttpClient<IAuthenticationService, SupabaseAuthenticationService>((provider, client) =>
        {
            var supabase = provider.GetRequiredService<SupabaseOptions>();
            client.BaseAddress = new Uri(supabase.Url, "auth/v1/");
            client.DefaultRequestHeaders.Add("apikey", supabase.PublishableKey);
            // Finish before the mobile client's 20-second deadline; never retry signup or token rotation here.
            client.Timeout = TimeSpan.FromSeconds(15);
        });
        services.AddScoped<IUsuarioRepository, UsuarioRepository>();
        services.AddScoped<IOnboardingRepository, OnboardingRepository>();
        services.AddScoped<IAlimentacaoRepository, AlimentacaoRepository>();
        services.AddScoped<IPlanoAlimentarRepository, PlanoAlimentarRepository>();
        services.AddScoped<IEvolucaoRepository, EvolucaoRepository>();
        services.AddScoped<IHabitosRepository, HabitosRepository>();
        services.AddScoped<IPremiumRepository, PremiumRepository>();
        services.AddScoped<IMarketplaceRepository, MarketplaceRepository>();
        services.AddScoped<INotificacaoRepository, NotificacaoRepository>();
        services.AddScoped<IRelatoriosRepository, RelatoriosRepository>();
        services.AddScoped<ILgpdRepository, LgpdRepository>();
        services.AddScoped<ITreinoRepository, TreinoRepository>();
        services.AddScoped<IDashboardRepository, DashboardRepository>();
        services.AddScoped<IAiUserContextBuilder, AiUserContextBuilder>();
        services.AddScoped<IAiFeatureFlagService, AiFeatureFlagService>();
        services.AddScoped<IAiCoachRepository, AiCoachRepository>();
        services.AddScoped<IAdminRepository, AdminRepository>();
        services.AddScoped<AdminIaRepository>();
        services.AddScoped<IAdminIaRepository>(provider => provider.GetRequiredService<AdminIaRepository>());
        services.AddScoped<IAiInstructionRepository>(provider => provider.GetRequiredService<AdminIaRepository>());
        services.AddHttpContextAccessor();
        services.AddTransient<AiCorrelationHandler>();
        services.AddHttpClient<IAiCoachClient, AiCoachHttpClient>((provider, client) =>
        {
            AiCoachOptions options = provider.GetRequiredService<IOptions<AiCoachOptions>>().Value;
            if (Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out Uri? baseAddress))
            {
                client.BaseAddress = baseAddress;
            }

            int timeoutSeconds = options.TimeoutSeconds <= 0 ? 30 : Math.Clamp(options.TimeoutSeconds, 5, 120);
            client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);

            if (!string.IsNullOrWhiteSpace(options.ApiKey))
            {
                client.DefaultRequestHeaders.Add("X-API-Key", options.ApiKey);
            }
        }).AddHttpMessageHandler<AiCorrelationHandler>();
        services.AddHttpClient<IAdminIaHealthClient, AdminIaHealthClient>((provider, client) =>
        {
            AiCoachOptions options = provider.GetRequiredService<IOptions<AiCoachOptions>>().Value;
            if (Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out Uri? baseAddress))
            {
                client.BaseAddress = baseAddress;
            }

            int timeoutSeconds = options.TimeoutSeconds <= 0 ? 30 : Math.Clamp(options.TimeoutSeconds, 5, 120);
            client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);

            if (!string.IsNullOrWhiteSpace(options.ApiKey))
            {
                client.DefaultRequestHeaders.Add("X-API-Key", options.ApiKey);
            }
        }).AddHttpMessageHandler<AiCorrelationHandler>();

        return services;
    }
}
