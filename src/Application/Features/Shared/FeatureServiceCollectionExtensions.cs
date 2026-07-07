using Microsoft.Extensions.DependencyInjection;

namespace Application.Features.Shared;

public static class FeatureServiceCollectionExtensions
{
    public static IServiceCollection AddFeatureRepositories(this IServiceCollection services)
    {
        services.AddScoped<SharedRepository>();
        services.AddScoped<Users.UserRepository>();
        services.AddScoped<ChatThreads.ChatThreadRepository>();
        services.AddScoped<ChatMessages.ChatMessageRepository>();
        services.AddScoped<UserSettings.UserSettingsRepository>();

        return services;
    }
}
