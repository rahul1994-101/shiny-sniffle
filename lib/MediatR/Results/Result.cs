using System.Collections.ObjectModel;

namespace MediatR.Results;

public class Result
{
    public Result()
    {
        HasError = false;
        Errors = new Collection<Error>();
    }

    public bool HasError { get; protected set; }

    public Collection<Error> Errors { get; }

    public string? FirstErrorMessage =>
        HasError && Errors.Count > 0 ? Errors[0].Message : null;


    public virtual void Success()
    {
        HasError = false;
        Errors.Clear();
    }

    public virtual void Failure(ErrorCode code, string message)
    {
        HasError = true;
        Errors.Add(new Error(code, message));
    }
}

public class Result<T> : Result
{
    public T? Payload { get; protected set; }

    public void Success(T? payload)
    {
        base.Success();
        Payload = payload;
    }
}
