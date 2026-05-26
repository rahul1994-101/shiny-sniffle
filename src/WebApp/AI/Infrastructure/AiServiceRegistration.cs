using WebApp.AI.Agents.Chat;
using WebApp.AI.Agents.Intent;
using WebApp.AI.Configuration;
using WebApp.AI.Memory;
using WebApp.AI.Orchestration;

namespace WebApp.AI.Infrastructure;

public static class AiServiceRegistration
{
    public static IServiceCollection AddAiServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<FoundryOptions>(configuration.GetSection(FoundryOptions.SectionName));
        services.AddSingleton<AgentFactory>();

        services.AddScoped<ThreadMemoryProvider>();
        services.AddScoped<IntentClassificationAgent>();
        services.AddScoped<GeneralChatAgent>();
        services.AddScoped<IntentRouter>();
        services.AddScoped<ChatOrchestrator>();

        return services;
    }
}
