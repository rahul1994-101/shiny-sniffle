using System.Reflection;
using MediatR.Abstractions;
using MediatR.Behaviors;
using MediatR.Dispatch;
using MediatR.Pipeline;
using MediatR.Results;
using Microsoft.Extensions.DependencyInjection;

namespace MediatR.DependencyInjection;

public static class MediatRServiceCollectionExtensions
{
    public static IServiceCollection AddMediatR(this IServiceCollection services, Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(assembly);

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
        var payloadHandlerType = typeof(IRequestHandler<,>);
        var commandHandlerType = typeof(IRequestHandler<>);
        var invokerType = typeof(RequestInvoker<,>);

        foreach (var type in assembly.GetTypes())
        {
            if (type is not { IsClass: true, IsAbstract: false })
            {
                continue;
            }

            foreach (var serviceType in type.GetInterfaces())
            {
                if (!serviceType.IsGenericType)
                {
                    continue;
                }

                var genericDefinition = serviceType.GetGenericTypeDefinition();

                if (genericDefinition == payloadHandlerType)
                {
                    var requestType = serviceType.GetGenericArguments()[0];
                    var responseType = serviceType.GetGenericArguments()[1];
                    var resultType = typeof(Result<>).MakeGenericType(responseType);
                    RegisterInvoker(table, invokerType, requestType, resultType);
                    continue;
                }

                if (genericDefinition == commandHandlerType)
                {
                    var requestType = serviceType.GetGenericArguments()[0];
                    RegisterInvoker(table, invokerType, requestType, typeof(Result));
                }
            }
        }

        return table;
    }

    private static void RegisterInvoker(
        RequestDispatchTable table,
        Type invokerType,
        Type requestType,
        Type resultType)
    {
        var closedInvokerType = invokerType.MakeGenericType(requestType, resultType);
        var invoker = (IRequestInvoker)Activator.CreateInstance(closedInvokerType)!;
        table.Register(invoker);
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
        var payloadHandlerType = typeof(IRequestHandler<,>);
        var commandHandlerType = typeof(IRequestHandler<>);
        var executorType = typeof(IRequestExecutor<,>);
        var payloadAdapterType = typeof(RequestHandlerAdapter<,>);
        var commandAdapterType = typeof(CommandRequestHandlerAdapter<>);
        var registeredHandlers = new Dictionary<Type, Type>();

        foreach (var type in assembly.GetTypes())
        {
            if (type is not { IsClass: true, IsAbstract: false })
            {
                continue;
            }

            foreach (var serviceType in type.GetInterfaces())
            {
                if (!serviceType.IsGenericType)
                {
                    continue;
                }

                var genericDefinition = serviceType.GetGenericTypeDefinition();

                if (genericDefinition == payloadHandlerType)
                {
                    var requestType = serviceType.GetGenericArguments()[0];
                    var responseType = serviceType.GetGenericArguments()[1];
                    var resultType = typeof(Result<>).MakeGenericType(responseType);

                    EnsureUniqueRequestHandler(registeredHandlers, requestType, type);

                    services.AddScoped(serviceType, type);
                    services.AddScoped(
                        executorType.MakeGenericType(requestType, resultType),
                        sp => Activator.CreateInstance(
                            payloadAdapterType.MakeGenericType(requestType, responseType),
                            sp.GetRequiredService(serviceType))!);
                    continue;
                }

                if (genericDefinition == commandHandlerType)
                {
                    var requestType = serviceType.GetGenericArguments()[0];

                    EnsureUniqueRequestHandler(registeredHandlers, requestType, type);

                    services.AddScoped(serviceType, type);
                    services.AddScoped(
                        executorType.MakeGenericType(requestType, typeof(Result)),
                        sp => Activator.CreateInstance(
                            commandAdapterType.MakeGenericType(requestType),
                            sp.GetRequiredService(serviceType))!);
                }
            }
        }
    }

    private static void EnsureUniqueRequestHandler(
        Dictionary<Type, Type> registeredHandlers,
        Type requestType,
        Type handlerType)
    {
        if (registeredHandlers.TryGetValue(requestType, out var existingHandler))
        {
            throw new InvalidOperationException(
                $"Duplicate IRequestHandler registration for {requestType.Name}: " +
                $"'{existingHandler.Name}' and '{handlerType.Name}' both handle this request.");
        }

        registeredHandlers[requestType] = handlerType;
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
