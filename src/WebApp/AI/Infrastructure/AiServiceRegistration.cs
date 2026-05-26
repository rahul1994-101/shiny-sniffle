using WebApp.AI.Agents.Intent;
using WebApp.AI.Configuration;
using WebApp.AI.Memory;
using WebApp.AI.Orchestration;
using WebApp.AI.Skills.General;
using WebApp.AI.Tools;
using WebApp.AI.Workflows.Workspace;

namespace WebApp.AI.Infrastructure;

public static class AiServiceRegistration
{
    public static IServiceCollection AddAiServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<FoundryOptions>(configuration.GetSection(FoundryOptions.SectionName));
        services.AddSingleton<AgentFactory>();

        services.AddScoped<ThreadMemoryProvider>();
        services.AddScoped<WorkspaceTools>();
        services.AddScoped<IntentAgent>();
        services.AddScoped<GeneralSkill>();
        services.AddScoped<WorkspaceInfoWorkflow>();
        services.AddScoped<IntentRouter>();
        services.AddScoped<ChatOrchestrator>();

        return services;
    }
}
