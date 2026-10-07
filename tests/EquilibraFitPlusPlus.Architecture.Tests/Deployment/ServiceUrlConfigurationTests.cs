using Microsoft.Extensions.Configuration;

namespace EquilibraFitPlusPlus.Architecture.Tests.Deployment;

/// <summary>Checks public service defaults and deployment overrides.</summary>
public sealed class ServiceUrlConfigurationTests
{
    /// <summary>Production must not inherit a local development destination.</summary>
    [Theory]
    [InlineData("src/backend/EquilibraFitPlusPlus.Api", "AiCoach:BaseUrl", "https://equilibrafit-plusplus-ai-4lkw.onrender.com")]
    [InlineData("src/admin/EquilibraFitPlusPlus.Admin", "AdminApi:BaseUrl", "https://equilibrafit-plusplus-api-4lkw.onrender.com")]
    public void Production_ShouldUsePublishedServiceUrl(string project, string key, string expected)
    {
        using var configuration = (ConfigurationRoot)ProjectConfiguration(project).Build();
        Assert.Equal(expected, configuration[key]);
        var uri = new Uri(configuration[key]!);
        Assert.Equal("https", uri.Scheme);
        Assert.False(uri.IsLoopback);
    }

    /// <summary>Render environment values remain authoritative after JSON defaults.</summary>
    [Theory]
    [InlineData("src/backend/EquilibraFitPlusPlus.Api", "AiCoach:BaseUrl", "AiCoach__BaseUrl")]
    [InlineData("src/admin/EquilibraFitPlusPlus.Admin", "AdminApi:BaseUrl", "AdminApi__BaseUrl")]
    public void Environment_ShouldOverrideProductionUrl(string project, string key, string variable)
    {
        string prefix = $"EFPP_URL_TEST_{Guid.NewGuid():N}_";
        const string expected = "https://override.example.test";
        Environment.SetEnvironmentVariable(prefix + variable, expected);
        try
        {
            using var configuration = (ConfigurationRoot)ProjectConfiguration(project).AddEnvironmentVariables(prefix).Build();
            Assert.Equal(expected, configuration[key]);
        }
        finally
        {
            Environment.SetEnvironmentVariable(prefix + variable, null);
        }
    }

    private static IConfigurationBuilder ProjectConfiguration(string project)
    {
        for (DirectoryInfo? root = new(AppContext.BaseDirectory); root is not null; root = root.Parent)
        {
            if (File.Exists(Path.Combine(root.FullName, "EquilibraFitPlusPlus.sln")))
                return new ConfigurationBuilder()
                    .SetBasePath(Path.Combine(root.FullName, project))
                    .AddJsonFile("appsettings.json")
                    .AddJsonFile("appsettings.Production.json");
        }
        throw new DirectoryNotFoundException("EquilibraFit++ solution root not found.");
    }
}
