namespace EquilibraFitPlusPlus.Architecture.Tests.Deployment;

/// <summary>Protects the independent Android identity and release signing boundary.</summary>
public sealed class AndroidReleaseConfigurationTests
{
    /// <summary>New release and debug IDs cannot replace the original Play application.</summary>
    [Fact]
    public void AndroidIdentity_ShouldUseApprovedPackageAndSeparateDebug()
    {
        var properties = Read("src/mobile/equilibrafit_plusplus_app/android/gradle.properties");
        Assert.Contains("PLUSPLUS_APPLICATION_ID=br.com.equilibrafit.app.plusplus", properties);
        var gradle = Read("src/mobile/equilibrafit_plusplus_app/android/app/build.gradle.kts");
        Assert.Contains("applicationIdSuffix = \".dev\"", gradle);
        Assert.Contains("plusplusApplicationId == originalApplicationId", gradle);
        Assert.Contains("releaseRequested && !hasReleaseSigning", gradle);
        Assert.Contains("signingConfig = signingConfigs.getByName(\"release\")", gradle);
        Assert.DoesNotContain("signingConfig = signingConfigs.getByName(\"debug\")", gradle);
    }

    /// <summary>Billing configuration is operator-owned and uses only server-side credentials.</summary>
    [Fact]
    public void Deployment_ShouldKeepGoogleCredentialsOutsideMobileAndImages()
    {
        var render = Read("render.yaml");
        Assert.Contains("key: GooglePlay__Enabled\n        sync: false", render);
        Assert.Contains("value: br.com.equilibrafit.app.plusplus", render);
        Assert.Contains("value: /etc/secrets/google-play.json", render);
        var ignore = Read(".gitignore");
        Assert.Contains("**/android/key.properties", ignore);
        Assert.Contains("*.jks", ignore);
        var dockerIgnore = Read(".dockerignore");
        Assert.Contains("*.jks", dockerIgnore);
        Assert.Contains("**/key.properties", dockerIgnore);
    }

    private static string Read(string relativePath)
    {
        for (DirectoryInfo? root = new(AppContext.BaseDirectory); root is not null; root = root.Parent)
        {
            if (File.Exists(Path.Combine(root.FullName, "EquilibraFitPlusPlus.sln")))
                return File.ReadAllText(Path.Combine(root.FullName, relativePath)).Replace("\r\n", "\n");
        }
        throw new DirectoryNotFoundException("EquilibraFit++ solution root not found.");
    }
}
