using System.Reflection;
using EquilibraFitPlusPlus.Api.Controllers;
using EquilibraFitPlusPlus.Domain.Entities;

namespace EquilibraFitPlusPlus.Architecture.Tests.CleanArchitecture;

/// <summary>
/// Tests Clean Architecture dependency direction.
/// </summary>
public sealed class DependencyDirectionTests
{
    /// <summary>
    /// Ensures the domain layer remains independent from outer layers.
    /// </summary>
    [Fact]
    public void Domain_ShouldNotReferenceOuterLayers()
    {
        Assembly assembly = typeof(Usuario).Assembly;

        AssertDoesNotReference(assembly, "EquilibraFitPlusPlus.Application", "EquilibraFitPlusPlus.Infrastructure", "EquilibraFitPlusPlus.Api", "EquilibraFitPlusPlus.Contracts");
    }

    /// <summary>
    /// Ensures the application layer does not depend on infrastructure or presentation.
    /// </summary>
    [Fact]
    public void Application_ShouldNotReferenceInfrastructureOrApi()
    {
        Assembly assembly = typeof(EquilibraFitPlusPlus.Application.DependencyInjection).Assembly;

        AssertDoesNotReference(assembly, "EquilibraFitPlusPlus.Infrastructure", "EquilibraFitPlusPlus.Api");
    }

    /// <summary>
    /// Ensures infrastructure does not depend on presentation.
    /// </summary>
    [Fact]
    public void Infrastructure_ShouldNotReferenceApi()
    {
        Assembly assembly = typeof(EquilibraFitPlusPlus.Infrastructure.DependencyInjection).Assembly;

        AssertDoesNotReference(assembly, "EquilibraFitPlusPlus.Api");
    }

    /// <summary>
    /// Ensures presentation remains the composition root for application and infrastructure.
    /// </summary>
    [Fact]
    public void Api_ShouldReferenceApplicationAndInfrastructure()
    {
        Assembly assembly = typeof(AuthController).Assembly;
        string[] references = GetReferencedAssemblyNames(assembly);

        Assert.Contains("EquilibraFitPlusPlus.Application", references);
        Assert.Contains("EquilibraFitPlusPlus.Infrastructure", references);
    }

    private static void AssertDoesNotReference(Assembly assembly, params string[] forbiddenAssemblies)
    {
        string[] references = GetReferencedAssemblyNames(assembly);

        foreach (string forbiddenAssembly in forbiddenAssemblies)
        {
            Assert.DoesNotContain(forbiddenAssembly, references);
        }
    }

    private static string[] GetReferencedAssemblyNames(Assembly assembly)
    {
        return assembly.GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .Where(name => name is not null)
            .Cast<string>()
            .ToArray();
    }
}
