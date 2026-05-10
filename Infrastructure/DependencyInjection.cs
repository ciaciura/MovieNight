using Infrastructure.External.TMDB;
using Infrastructure.Data;
using Infrastructure.Services;

namespace Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
        {
            services.AddSingleton<TMDBService>();
            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseNpgsql(config.GetConnectionString("DefaultConnection"));
            });
            services.AddScoped<ITwoFactorService, TwoFactorService>();
            services.AddScoped<IEmailService, LogEmailService>();
            return services;
        }
    }
}
