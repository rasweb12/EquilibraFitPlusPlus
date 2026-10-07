using System.Xml;
using System.Xml.Linq;

namespace EquilibraFitPlusPlus.Architecture.Tests.Deployment;

/// <summary>Protects the email confirmation template and Android return destinations.</summary>
public sealed class EmailConfirmationConfigurationTests
{
    /// <summary>The signup email is localized and identifies the independent product.</summary>
    [Fact]
    public void Template_ShouldUsePortugueseAndProductBranding()
    {
        var template = ReadXml("infra/supabase/templates/confirmation.html");
        Assert.Equal("pt-BR", template.Root!.Attribute("lang")?.Value);
        Assert.Equal("ltr", template.Root.Attribute("dir")?.Value);
        Assert.Equal("Confirme seu e-mail - EquilibraFit++", template.Descendants("title").Single().Value);
        Assert.Equal("Confirme seu e-mail", template.Descendants("h1").Single().Value);
        Assert.Contains("EquilibraFit++", template.Descendants("body").Single().Value);
    }

    /// <summary>Every recipient receives a provider-generated link, never a copied token or local URL.</summary>
    [Fact]
    public void Template_ShouldPreserveProviderConfirmationLink()
    {
        var template = ReadXml("infra/supabase/templates/confirmation.html");
        var link = template.Descendants("a").Single();
        Assert.Equal("{{ .ConfirmationURL }}", link.Attribute("href")?.Value);
        Assert.Equal("Confirmar meu e-mail", link.Value);
        Assert.DoesNotContain("localhost", template.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("token=", template.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.Empty(template.Descendants("script"));
        Assert.Empty(template.Descendants("form"));
    }

    /// <summary>Android can open login after confirmation while retaining the existing recovery flow.</summary>
    [Theory]
    [InlineData("path", "/login")]
    [InlineData("pathPrefix", "/recovery")]
    public void Android_ShouldAcceptAuthReturnDestination(string pathAttribute, string path)
    {
        XNamespace android = "http://schemas.android.com/apk/res/android";
        var manifest = ReadXml("src/mobile/equilibrafit_plusplus_app/android/app/src/main/AndroidManifest.xml");
        var activity = manifest.Descendants("activity")
            .Single(x => x.Attribute(android + "name")?.Value == ".MainActivity");
        Assert.Equal("true", activity.Attribute(android + "exported")?.Value);
        var filter = activity.Elements("intent-filter").Single(x => x.Elements("data").Any(data =>
            data.Attribute(android + "scheme")?.Value == "equilibrafitplusplus" &&
            data.Attribute(android + "host")?.Value == "auth" &&
            data.Attribute(android + pathAttribute)?.Value == path));
        Assert.Contains(filter.Elements("action"), x => x.Attribute(android + "name")?.Value == "android.intent.action.VIEW");
        Assert.Contains(filter.Elements("category"), x => x.Attribute(android + "name")?.Value == "android.intent.category.DEFAULT");
        Assert.Contains(filter.Elements("category"), x => x.Attribute(android + "name")?.Value == "android.intent.category.BROWSABLE");
    }

    private static XDocument ReadXml(string relativePath)
    {
        for (DirectoryInfo? root = new(AppContext.BaseDirectory); root is not null; root = root.Parent)
        {
            if (!File.Exists(Path.Combine(root.FullName, "EquilibraFitPlusPlus.sln"))) continue;
            using var reader = XmlReader.Create(Path.Combine(root.FullName, relativePath),
                new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore, XmlResolver = null });
            return XDocument.Load(reader);
        }
        throw new DirectoryNotFoundException("EquilibraFit++ solution root not found.");
    }
}
