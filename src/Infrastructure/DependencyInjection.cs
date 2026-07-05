using Infrastructure.Foundry;
using Infrastructure.Mailbox;
using Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddDbContextFactory<AppDbContext>();
        services.AddScoped<IMailboxService, MailKitMailboxService>();
        services.AddSingleton<IFoundryAgentFactory, FoundryAgentFactory>();

        return services;
    }
}
