using System.Reflection;

using Application.AI;
using Application.AI.Agents;
using Application.AI.Memory;
using Application.AI.Tools;
using Application.Features.ChatMessages;
using Application.Features.ChatThreads;
using Application.Features.Shared;
using Application.Features.EmailAccounts;
using Application.Features.EmailProviders;
using Application.Features.UserSettings;
using Application.Features.Users;
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

        #region Feature repositories

        services.AddScoped<SharedRepository>();
        services.AddScoped<UserRepository>();
        services.AddScoped<ChatThreadRepository>();
        services.AddScoped<ChatMessageRepository>();
        services.AddScoped<UserSettingsRepository>();
        services.AddScoped<EmailProviderRepository>();
        services.AddScoped<EmailAccountRepository>();

        #endregion

        #region Feature services (Shared/Services.cs)

        services.AddScoped<UserMailboxService>();

        #endregion

        services.AddMediatR(Assembly.GetExecutingAssembly());

        services.Configure<FoundryOptions>(configuration.GetSection(FoundryOptions.SectionName));

        #region AI

        services.AddScoped<EmailTriageTools>();
        services.AddScoped<EmailTriageAgent>();
        services.AddScoped<ThreadMemoryService>();
        services.AddScoped<AssistantAgent>();
        services.AddScoped<ChatOrchestrator>();

        #endregion

        return services;
    }
}
