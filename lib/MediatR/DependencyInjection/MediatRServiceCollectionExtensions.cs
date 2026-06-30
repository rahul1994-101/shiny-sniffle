using System.Reflection;
using FluentValidation;
using MediatR.Abstractions;
using MediatR.Behaviors;
using MediatR.Dispatch;
using MediatR.Pipeline;
using Microsoft.Extensions.DependencyInjection;

namespace MediatR.DependencyInjection;

public static class MediatRServiceCollectionExtensions
{
    public static IServiceCollection AddMediatR(this IServiceCollection services, Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(assembly);

        services.AddValidatorsFromAssembly(assembly);

        services.AddScoped(typeof(RequestPipeline<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ExceptionBehavior<,>));

        services.AddScoped<RequestDispatcherCore>();
        services.AddSingleton(BuildRequestDispatchTable(assembly));
        services.AddSingleton(BuildNotificationDispatchTable(assembly));
        services.AddScoped<IMediator, Mediator>();

        RegisterRequestHandlers(services, assembly);
        RegisterNotificationHandlers(services, assembly);

        return services;
    }

    private static RequestDispatchTable BuildRequestDispatchTable(Assembly assembly)
    {
        var table = new RequestDispatchTable();
        var handlerType = typeof(IRequestHandler<,>);
        var invokerType = typeof(RequestInvoker<,>);

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
                var invoker = (IRequestInvoker)Activator.CreateInstance(closedInvokerType)!;
                table.Register(invoker);
            }
        }

        return table;
    }

    private static NotificationDispatchTable BuildNotificationDispatchTable(Assembly assembly)
    {
        var table = new NotificationDispatchTable();
        var handlerType = typeof(INotificationHandler<>);
        var invokerType = typeof(NotificationHandlerInvoker<>);
        var registered = new HashSet<Type>();

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

                var notificationType = serviceType.GetGenericArguments()[0];
                if (!registered.Add(notificationType))
                {
                    continue;
                }

                var closedInvokerType = invokerType.MakeGenericType(notificationType);
                var invoker = (INotificationHandlerInvoker)Activator.CreateInstance(closedInvokerType)!;
                table.Register(invoker);
            }
        }

        return table;
    }

    private static void RegisterRequestHandlers(IServiceCollection services, Assembly assembly)
    {
        var handlerType = typeof(IRequestHandler<,>);

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

    private static void RegisterNotificationHandlers(IServiceCollection services, Assembly assembly)
    {
        var handlerType = typeof(INotificationHandler<>);

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
