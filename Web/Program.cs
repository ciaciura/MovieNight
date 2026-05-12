using MovieNight.Web;
using MovieNight.Web.Components;
using MovieNight.Web.Auth;
using MovieNight.Web.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Options;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthorizationCore();
builder.Services.AddOutputCache();
builder.Services.AddScoped<AuthSession>();
builder.Services.AddScoped<MovieNightAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(serviceProvider =>
    serviceProvider.GetRequiredService<MovieNightAuthenticationStateProvider>());
builder.Services.AddScoped<ApiAccessTokenHandler>();

builder.Services.AddHttpClient(AuthApiClient.AnonymousClientName, client =>
    {
        client.BaseAddress = new("https+http://apiservice");
    });

builder.Services.AddHttpClient(AuthApiClient.AuthenticatedClientName, client =>
    {
        client.BaseAddress = new("https+http://apiservice");
    })
    .AddHttpMessageHandler<ApiAccessTokenHandler>();

builder.Services.AddScoped<AuthApiClient>();
builder.Services.AddScoped<IAskAndRunService, AskAndRunService>();
builder.Services.AddScoped<ILocalizationService, LocalizationService>();

// Add localization services
var supportedCultures = new[] 
{
    new CultureInfo("en-US"),
    new CultureInfo("pl-PL")
};

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.DefaultRequestCulture = new RequestCulture("en-US");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
    options.AddInitialRequestCultureProvider(new CustomRequestCultureProvider(async context =>
    {
        // Try to get culture from cookie first
        if (context.Request.Cookies.TryGetValue("Culture", out var culture))
        {
            return new ProviderCultureResult(culture);
        }
        
        // Fall back to browser language
        return await Task.FromResult(new ProviderCultureResult(context.Request.HttpContext.Features
            .Get<IRequestCultureFeature>()?.RequestCulture.Culture?.Name ?? "en-US"));
    }));
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

// Use localization middleware
var locOptions = app.Services.GetRequiredService<IOptions<RequestLocalizationOptions>>();
app.UseRequestLocalization(locOptions.Value);

app.UseAntiforgery();

app.UseOutputCache();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapDefaultEndpoints();

app.Run();
