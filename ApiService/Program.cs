using Infrastructure; // Ensure this is the correct namespace for AddInfrastructure
using ApiService.Endpoints.Modules;
using FluentValidation;
using Microsoft.OpenApi.Models;
using Scalar.AspNetCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MovieNight.Auth;
using Shared.Extensions.Users;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddProblemDetails();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwtSection = builder.Configuration.GetSection("Authentication:Jwt");
        var issuer = jwtSection["Issuer"];
        var audience = jwtSection["Audience"];
        var signingKey = jwtSection["SigningKey"];

        if (string.IsNullOrWhiteSpace(issuer) ||
            string.IsNullOrWhiteSpace(audience) ||
            string.IsNullOrWhiteSpace(signingKey))
        {
            throw new InvalidOperationException("JWT settings are missing in Authentication:Jwt.");
        }

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = issuer,
            ValidAudience = audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();

    options.AddPolicy(AdminApiKeyRequirement.PolicyName, policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireClaim(UserClaimTypes.Admin, bool.TrueString);
        policy.Requirements.Add(new AdminApiKeyRequirement());
    });
});
builder.Services.AddSingleton<IAuthorizationHandler, AdminApiKeyHandler>();
builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((doc, context, ct) =>
    {
        doc.Info = new OpenApiInfo
        {
            Title = "MovieNight API",
            Version = "v1",
            Description = """
                Internal API for the **MovieNight** application.

                ## Authentication
                Most endpoints require a **Bearer token** obtained from `POST /api/auth/token`.
                Include it as `Authorization: Bearer <token>` on every protected request.

                ## Admin endpoints
                Administrative operations additionally require:
                - The `admin` claim set to `true` in the JWT.
                - A valid `X-Api-Key` header matching the server's configured admin API key.

                ## Notes
                - This API is **non-public** and intended for internal use only.
                - All timestamps are UTC.
                - Validation errors return RFC 9457 `ValidationProblem` responses.
                """,
            Contact = new OpenApiContact
            {
                Name = "MovieNight Team"
            }
        };
        return Task.CompletedTask;
    });
});
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
    app.MapScalarApiReference(options =>
    {
        options.Title = "MovieNight API";
        options.WithPreferredScheme("Bearer");
    }).AllowAnonymous();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapApiEndpoints();

app.Run();

