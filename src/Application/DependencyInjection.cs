using System.Reflection;

using Application.AI;
using Application.AI.Agents;
using Application.AI.Memory;
using Application.AI.Tools;
using Application.Features.Shared;
using Application.Services;
using MediatR.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddFeatureRepositories();
        services.AddMediatR(Assembly.GetExecutingAssembly());

        services.Configure<FoundryOptions>(configuration.GetSection(FoundryOptions.SectionName));

        services.AddScoped<UserMailboxService>();
        services.AddScoped<EmailTools>();
        services.AddScoped<ThreadMemoryService>();
        services.AddScoped<AssistantAgent>();
        services.AddScoped<EmailAgent>();
        services.AddScoped<ChatOrchestrator>();

        return services;
    }
}
