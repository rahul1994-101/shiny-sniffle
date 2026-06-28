namespace WebApp.Features._Shared.Abstractions;

public interface IFeatureRequest<TResult> where TResult : AppResult, new() { }

public interface ICommand<TResult> : IFeatureRequest<TResult> where TResult : AppResult, new() { }

public interface IQuery<TResult> : IFeatureRequest<TResult> where TResult : AppResult, new() { }

public interface IFeatureHandler<in TRequest, TResult>
    where TRequest : IFeatureRequest<TResult>
    where TResult : AppResult, new()
{
    Task<TResult> HandleAsync(TRequest request, CancellationToken cancellationToken = default);
}

public delegate Task<TResult> FeatureHandlerDelegate<TRequest, TResult>(
    TRequest request,
    CancellationToken cancellationToken)
    where TRequest : IFeatureRequest<TResult>
    where TResult : AppResult, new();

public interface IFeaturePipelineBehavior<TRequest, TResult>
    where TRequest : IFeatureRequest<TResult>
    where TResult : AppResult, new()
{
    Task<TResult> HandleAsync(
        TRequest request,
        FeatureHandlerDelegate<TRequest, TResult> next,
        CancellationToken cancellationToken);
}

/// <summary>MediatR-style entry point: send a command/query; handler, validation, and pipeline run automatically.</summary>
public interface IFeatureSender
{
    Task<TResult> SendAsync<TResult>(
        IFeatureRequest<TResult> request,
        CancellationToken cancellationToken = default)
        where TResult : AppResult, new();
}
