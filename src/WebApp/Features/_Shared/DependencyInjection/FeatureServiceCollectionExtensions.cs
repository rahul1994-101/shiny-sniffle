using FluentValidation;
using System.Reflection;
using WebApp.Features._Shared.Abstractions;
using WebApp.Features._Shared.Behaviors;
using WebApp.Features._Shared.Dispatch;
using WebApp.Features._Shared.Pipeline;

namespace WebApp.Features._Shared.DependencyInjection;

public static class FeatureServiceCollectionExtensions
{
    public static IServiceCollection AddFeatureLayer(this IServiceCollection services, Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(assembly);

        services.AddValidatorsFromAssembly(assembly);

        services.AddScoped(typeof(FeaturePipeline<,>));
        services.AddScoped(typeof(IFeaturePipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddScoped(typeof(IFeaturePipelineBehavior<,>), typeof(ExceptionBehavior<,>));

        services.AddScoped<FeatureDispatcherCore>();
        services.AddSingleton(BuildDispatchTable(assembly));
        services.AddScoped<IFeatureSender, FeatureSender>();

        RegisterHandlers(services, assembly);

        return services;
    }

    public static IServiceCollection AddFeatureRepositories(this IServiceCollection services)
    {
        services.AddScoped<WebApp.Features.Shared.SharedRepository>();
        services.AddScoped<WebApp.Features.Users.UserRepository>();
        services.AddScoped<WebApp.Features.ChatThreads.ChatThreadRepository>();
        services.AddScoped<WebApp.Features.ChatMessages.ChatMessageRepository>();
        services.AddScoped<WebApp.Features.UserSettings.UserSettingsRepository>();

        return services;
    }

    private static FeatureDispatchTable BuildDispatchTable(Assembly assembly)
    {
        var table = new FeatureDispatchTable();
        var handlerType = typeof(IFeatureHandler<,>);
        var invokerType = typeof(FeatureRequestInvoker<,>);

        foreach (var type in assembly.GetTypes())
        {
            if (type is not { IsClass: true, IsAbstract: false })
            {
                continue;
            }

            foreach (var serviceType in type.GetInterfaces())
            {
                if (!serviceType.IsGenericType
                    || serviceType.GetGenericTypeDefinition() != handlerType)
                {
                    continue;
                }

                var requestType = serviceType.GetGenericArguments()[0];
                var resultType = serviceType.GetGenericArguments()[1];
                var closedInvokerType = invokerType.MakeGenericType(requestType, resultType);
                var invoker = (IFeatureRequestInvoker)Activator.CreateInstance(closedInvokerType)!;
                table.Register(invoker);
            }
        }

        return table;
    }

    private static void RegisterHandlers(IServiceCollection services, Assembly assembly)
    {
        var handlerType = typeof(IFeatureHandler<,>);

        foreach (var type in assembly.GetTypes())
        {
            if (type is not { IsClass: true, IsAbstract: false })
            {
                continue;
            }

            foreach (var serviceType in type.GetInterfaces())
            {
                if (serviceType.IsGenericType
                    && serviceType.GetGenericTypeDefinition() == handlerType)
                {
                    services.AddScoped(serviceType, type);
                }
            }
        }
    }
}
