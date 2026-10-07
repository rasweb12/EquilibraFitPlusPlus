using EquilibraFitPlusPlus.Admin.Components;
using EquilibraFitPlusPlus.Admin.Services;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);
string? configuredApiUrl = builder.Configuration["AdminApi:BaseUrl"];
if (!builder.Environment.IsDevelopment() &&
    (string.IsNullOrWhiteSpace(configuredApiUrl) || !Uri.TryCreate(configuredApiUrl, UriKind.Absolute, out var configuredUri)
        || configuredUri.IsLoopback))
    throw new InvalidOperationException("Configure AdminApi__BaseUrl for the deployed API.");

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddMudServices();
builder.Services.Configure<AdminApiOptions>(builder.Configuration.GetSection(AdminApiOptions.SectionName));
builder.Services.AddScoped<AdminSessionState>();
builder.Services.AddScoped<AdminApiClient>();
builder.Services.AddHttpClient(AdminApiClient.HttpClientName, client =>
{
    string baseUrl = builder.Configuration.GetSection(AdminApiOptions.SectionName)
        .GetValue<string>(nameof(AdminApiOptions.BaseUrl)) ?? "http://localhost:5158";
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAntiforgery();
app.MapGet("/health", () => Results.Ok(new { status = "Healthy", service = "equilibrafit-plusplus-admin" }));

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
