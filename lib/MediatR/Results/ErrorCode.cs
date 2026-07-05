namespace MediatR.Results;

public enum ErrorCode
{
    BadRequest = 400,
    NotFound = 404,
    InternalServerError = 500,

    UnknownError = 0
}
