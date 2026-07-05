namespace MediatR.Results;

public class Error
{
    public Error()
    {
    }

    public Error(ErrorCode code, string message)
    {
        Code = code;
        Message = message;
    }

    public ErrorCode Code { get; set; }

    public string Message { get; set; } = string.Empty;
}
