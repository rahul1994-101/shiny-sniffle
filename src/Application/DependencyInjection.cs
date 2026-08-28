using System.Reflection;

using Application.AI;
using Application.AI.Agents;
using Application.AI.Memory;
using Application.AI.Tools;
using Application.Features.Chat.ChatMessages;
using Application.Features.Chat.ChatThreads;
using Application.Features.Dbo.EmailProviders;
using Application.Features.Workspace.Contacts;
using Application.Features.Workspace.EmailAccounts;
using Application.Features.Workspace.Tags;
using Application.Features.Workspace.Buckets;
using Application.Features.Dbo.Users;
using Infrastructure.Foundry;
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
        services.AddScoped<EntityRefResolver>();
        services.AddScoped<EntityRefMentionContextService>();
        services.AddScoped<UserRepository>();
        services.AddScoped<ChatThreadRepository>();
        services.AddScoped<ChatMessageRepository>();
        services.AddScoped<EmailProviderRepository>();
        services.AddScoped<EmailAccountRepository>();
        services.AddScoped<ContactRepository>();
        services.AddScoped<TagRepository>();
        services.AddScoped<BucketRepository>();

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
