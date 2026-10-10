using System.Reflection;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using EquilibraFitPlusPlus.Application.Common.Behaviors;

namespace EquilibraFitPlusPlus.Application;

/// <summary>
/// Registers application layer dependencies.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds application services to the container.
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services, string? mediatrLicenseKey = null)
    {
        Assembly assembly = typeof(DependencyInjection).Assembly;

        services.AddMediatR(configuration =>
        {
            configuration.RegisterServicesFromAssembly(assembly);
            configuration.LicenseKey = string.IsNullOrWhiteSpace(mediatrLicenseKey) ? null : mediatrLicenseKey;
        });
        services.AddValidatorsFromAssembly(assembly);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        return services;
    }
}
