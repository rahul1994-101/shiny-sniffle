using EntityFramework.Exceptions.SqlServer;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        services.AddDbContextFactory<AppDbContext>((sp, options) =>
        {
            options.UseSqlServer(connectionString);
            options.UseExceptionProcessor();

            if (sp.GetRequiredService<IHostEnvironment>().IsDevelopment())
            {
                options.EnableDetailedErrors();
                options.EnableSensitiveDataLogging();
            }
        });

        services.AddScoped<IMailboxService, MailKitMailboxService>();
        services.AddSingleton<IFoundryAgentFactory, FoundryAgentFactory>();

        return services;
    }
}
