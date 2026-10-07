using System.Reflection;
using EquilibraFitPlusPlus.Api.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EquilibraFitPlusPlus.Api.IntegrationTests.Controllers;

/// <summary>
/// Tests route contracts for authenticated user modules.
/// </summary>
public sealed class UserModulesControllerContractTests
{
    /// <summary>
    /// Ensures user module controllers require authentication.
    /// </summary>
    [Theory]
    [InlineData(typeof(OnboardingController), "api/v1/onboarding")]
    [InlineData(typeof(AlimentacaoController), "api/v1/alimentacao")]
    [InlineData(typeof(PlanosController), "api/v1/planos")]
    [InlineData(typeof(TreinosController), "api/v1/treinos")]
    [InlineData(typeof(DashboardController), "api/v1/dashboard")]
    [InlineData(typeof(AiCoachController), "api/v1/ia/coach")]
    [InlineData(typeof(PremiumController), "api/v1/premium")]
    [InlineData(typeof(MarketplaceController), "api/v1/marketplace")]
    [InlineData(typeof(NotificacoesController), "api/v1/notificacoes")]
    [InlineData(typeof(RelatoriosController), "api/v1/relatorios")]
    [InlineData(typeof(GamificacaoController), "api/v1/gamificacao")]
    [InlineData(typeof(LgpdController), "api/v1/lgpd")]
    public void UserModuleControllers_ShouldBeAuthorizedAndVersioned(Type controllerType, string routeTemplate)
    {
        var authorize = controllerType.GetCustomAttribute<AuthorizeAttribute>();
        var route = controllerType.GetCustomAttribute<RouteAttribute>();

        Assert.NotNull(authorize);
        Assert.NotNull(route);
        Assert.Equal(routeTemplate, route!.Template);
    }
}
