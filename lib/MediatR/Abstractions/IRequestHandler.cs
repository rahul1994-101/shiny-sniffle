using MediatR.Results;

namespace MediatR.Abstractions;

public interface IRequestHandler<in TRequest, TResponse>
    where TRequest : IRequest<Result<TResponse>>
{
    ValueTask<Result<TResponse>> HandleAsync(TRequest request, CancellationToken cancellationToken = default);
}

public interface IRequestHandler<in TRequest>
    where TRequest : ICommand
{
    ValueTask<Result> HandleAsync(TRequest request, CancellationToken cancellationToken = default);
}
