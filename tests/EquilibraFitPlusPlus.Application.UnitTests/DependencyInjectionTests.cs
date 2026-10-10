using EquilibraFitPlusPlus.Application;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace EquilibraFitPlusPlus.Application.UnitTests;

public sealed class DependencyInjectionTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void AddApplication_ShouldNotInventLicenseWhenUnconfigured(string? key)
    {
        var services = new ServiceCollection();
        services.AddApplication(key);
        var registration = Assert.Single(services, service => service.ServiceType == typeof(MediatRServiceConfiguration));
        var configuration = Assert.IsType<MediatRServiceConfiguration>(registration.ImplementationInstance);
        Assert.Null(configuration.LicenseKey);
        Assert.Contains(services, service => service.ServiceType == typeof(ISender));
    }

    [Fact]
    public void AddApplication_ShouldPassConfiguredLicenseWithoutChangingHandlerRegistration()
    {
        const string placeholder = "test-license-not-real";
        var services = new ServiceCollection();
        services.AddApplication(placeholder);
        var registration = Assert.Single(services, service => service.ServiceType == typeof(MediatRServiceConfiguration));
        var configuration = Assert.IsType<MediatRServiceConfiguration>(registration.ImplementationInstance);
        Assert.Equal(placeholder, configuration.LicenseKey);
        Assert.Contains(services, service => service.ServiceType == typeof(ISender));
    }
}
