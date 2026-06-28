using WebApp.Features._Shared.Abstractions;
using WebApp.Features._Shared.Pipeline;

namespace WebApp.Features._Shared.Dispatch;

internal interface IFeatureRequestInvoker
{
    Type RequestType { get; }

    Task<AppResult> InvokeAsync(IServiceProvider services, object request, CancellationToken cancellationToken);
}

internal sealed class FeatureRequestInvoker<TRequest, TResult> : IFeatureRequestInvoker
    where TRequest : IFeatureRequest<TResult>
    where TResult : AppResult, new()
{
    public Type RequestType => typeof(TRequest);

    public async Task<AppResult> InvokeAsync(
        IServiceProvider services,
        object request,
        CancellationToken cancellationToken)
    {
        var core = services.GetRequiredService<FeatureDispatcherCore>();
        return await core.SendAsync<TRequest, TResult>((TRequest)request, cancellationToken);
    }
}

public sealed class FeatureDispatchTable
{
    private readonly Dictionary<Type, IFeatureRequestInvoker> _invokers = new();

    internal void Register(IFeatureRequestInvoker invoker) =>
        _invokers[invoker.RequestType] = invoker;

    internal IFeatureRequestInvoker GetRequired(Type requestType) =>
        _invokers.TryGetValue(requestType, out var invoker)
            ? invoker
            : throw new InvalidOperationException($"No feature handler registered for {requestType.Name}.");
}

public sealed class FeatureDispatcherCore(IServiceProvider services)
{
    public Task<TResult> SendAsync<TRequest, TResult>(TRequest request, CancellationToken cancellationToken)
        where TRequest : IFeatureRequest<TResult>
        where TResult : AppResult, new()
    {
        var handler = services.GetRequiredService<IFeatureHandler<TRequest, TResult>>();
        var pipeline = services.GetRequiredService<FeaturePipeline<TRequest, TResult>>();
        return pipeline.ExecuteAsync(request, handler, cancellationToken);
    }
}

public sealed class FeatureSender(IServiceProvider services, FeatureDispatchTable table) : IFeatureSender
{
    public async Task<TResult> SendAsync<TResult>(
        IFeatureRequest<TResult> request,
        CancellationToken cancellationToken = default)
        where TResult : AppResult, new()
    {
        ArgumentNullException.ThrowIfNull(request);

        var invoker = table.GetRequired(request.GetType());
        var result = await invoker.InvokeAsync(services, request, cancellationToken);
        return (TResult)result;
    }
}
