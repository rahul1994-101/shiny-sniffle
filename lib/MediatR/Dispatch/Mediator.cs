using MediatR.Abstractions;
using MediatR.Pipeline;
using MediatR.Results;
using Microsoft.Extensions.DependencyInjection;

namespace MediatR.Dispatch;

internal interface IRequestInvoker
{
    Type RequestType { get; }

    ValueTask<Result> InvokeAsync(IServiceProvider services, object request, CancellationToken cancellationToken);
}

internal sealed class RequestInvoker<TRequest, TResult> : IRequestInvoker
    where TRequest : IRequest<TResult>
    where TResult : Result, new()
{
    public Type RequestType => typeof(TRequest);

    public async ValueTask<Result> InvokeAsync(
        IServiceProvider services,
        object request,
        CancellationToken cancellationToken)
    {
        var core = services.GetRequiredService<RequestDispatcherCore>();
        return await core.SendAsync<TRequest, TResult>((TRequest)request, cancellationToken);
    }
}

internal sealed class RequestDispatchTable
{
    private readonly Dictionary<Type, IRequestInvoker> _invokers = new();

    internal void Register(IRequestInvoker invoker) =>
        _invokers[invoker.RequestType] = invoker;

    internal IRequestInvoker GetRequired(Type requestType) =>
        _invokers.TryGetValue(requestType, out var invoker)
            ? invoker
            : throw new InvalidOperationException($"No handler registered for {requestType.Name}.");
}

internal sealed class RequestDispatcherCore(IServiceProvider services)
{
    public ValueTask<TResult> SendAsync<TRequest, TResult>(TRequest request, CancellationToken cancellationToken)
        where TRequest : IRequest<TResult>
        where TResult : Result, new()
    {
        var handler = services.GetRequiredService<IRequestHandler<TRequest, TResult>>();
        var pipeline = services.GetRequiredService<RequestPipeline<TRequest, TResult>>();
        return pipeline.ExecuteAsync(request, handler, cancellationToken);
    }
}

internal interface INotificationHandlerInvoker
{
    Type NotificationType { get; }

    ValueTask InvokeAsync(IServiceProvider services, INotification notification, CancellationToken cancellationToken);
}

internal sealed class NotificationHandlerInvoker<TNotification> : INotificationHandlerInvoker
    where TNotification : INotification
{
    public Type NotificationType => typeof(TNotification);

    public async ValueTask InvokeAsync(
        IServiceProvider services,
        INotification notification,
        CancellationToken cancellationToken)
    {
        var handlers = services.GetServices<INotificationHandler<TNotification>>();
        foreach (var handler in handlers)
        {
            await handler.HandleAsync((TNotification)notification, cancellationToken);
        }
    }
}

internal sealed class NotificationDispatchTable
{
    private readonly Dictionary<Type, INotificationHandlerInvoker> _invokers = new();

    internal void Register(INotificationHandlerInvoker invoker) =>
        _invokers[invoker.NotificationType] = invoker;

    internal INotificationHandlerInvoker? Find(Type notificationType) =>
        _invokers.GetValueOrDefault(notificationType);
}

internal sealed class Mediator(
    IServiceProvider services,
    RequestDispatchTable requestTable,
    NotificationDispatchTable notificationTable) : IMediator
{
    public async ValueTask<TResult> SendAsync<TResult>(
        IRequest<TResult> request,
        CancellationToken cancellationToken = default)
        where TResult : Result, new()
    {
        ArgumentNullException.ThrowIfNull(request);

        var invoker = requestTable.GetRequired(request.GetType());
        var result = await invoker.InvokeAsync(services, request, cancellationToken);
        return (TResult)result;
    }

    public async ValueTask PublishAsync(INotification notification, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notification);

        var invoker = notificationTable.Find(notification.GetType());
        if (invoker is null)
        {
            return;
        }

        await invoker.InvokeAsync(services, notification, cancellationToken);
    }
}
