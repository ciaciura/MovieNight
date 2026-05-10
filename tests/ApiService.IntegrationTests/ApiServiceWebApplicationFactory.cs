using Infrastructure.Data;
using Infrastructure.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace ApiService.IntegrationTests;

public sealed class ApiServiceWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection sqliteConnection;

    public ApiServiceWebApplicationFactory()
    {
        sqliteConnection = new SqliteConnection("DataSource=:memory:");
        sqliteConnection.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, configurationBuilder) =>
        {
            var settings = new Dictionary<string, string?>
            {
                ["Authentication:Jwt:Issuer"] = "MovieNight.Api.Tests",
                ["Authentication:Jwt:Audience"] = "MovieNight.Tests.Clients",
                ["Authentication:Jwt:SigningKey"] = "THIS_IS_A_LONG_TEST_SIGNING_KEY_CHANGE_ME",
                ["Authentication:Jwt:TokenLifetimeMinutes"] = "60",
                ["Authentication:AdminApiKey"] = "test-admin-key",
                ["Authentication:AdminUsers:0"] = "admin"
            };

            configurationBuilder.AddInMemoryCollection(settings);
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
            services.RemoveAll<ITwoFactorService>();
            services.RemoveAll<IEmailService>();

            services.AddDbContext<AppDbContext>(options => options.UseSqlite(sqliteConnection));
            services.AddSingleton<ITwoFactorService, FakeTwoFactorService>();
            services.AddSingleton<IEmailService, FakeEmailService>();
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            sqliteConnection.Dispose();
        }
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        using var scope = host.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        dbContext.Database.EnsureCreated();

        return host;
    }

    private sealed class FakeTwoFactorService : ITwoFactorService
    {
        public string GenerateTotpSecret() => "BASE32TESTSECRET";

        public string GetTotpSetupUri(string issuer, string accountName, string base32Secret)
            => $"otpauth://totp/{Uri.EscapeDataString(issuer)}:{Uri.EscapeDataString(accountName)}?secret={base32Secret}&issuer={Uri.EscapeDataString(issuer)}&algorithm=SHA1&digits=6&period=30";

        public bool VerifyTotp(string base32Secret, string code)
            => base32Secret == "BASE32TESTSECRET" && code == "123456";

        public (string Code, string CodeHash) GenerateEmailOtp() => ("654321", "HASHED_654321");

        public bool VerifyEmailOtpHash(string code, string storedHash)
            => code == "654321" && storedHash == "HASHED_654321";
    }

    private sealed class FakeEmailService : IEmailService
    {
        public Task SendOtpAsync(string toEmail, string displayName, string otp, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
