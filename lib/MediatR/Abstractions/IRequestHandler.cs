using MediatR.Results;

namespace MediatR.Abstractions;

public interface IRequestHandler<in TRequest, TResult>
    where TRequest : IRequest<TResult>
    where TResult : Result, new()
{
    ValueTask<TResult> HandleAsync(TRequest request, CancellationToken cancellationToken = default);
}
