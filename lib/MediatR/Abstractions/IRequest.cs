using MediatR.Results;

namespace MediatR.Abstractions;

public interface IRequest<TResult> where TResult : Result, new() { }

public interface ICommand : IRequest<Result> { }

public interface ICommand<TResponse> : IRequest<Result<TResponse>> { }

public interface IQuery<TResponse> : IRequest<Result<TResponse>> { }

public interface INotification { }
