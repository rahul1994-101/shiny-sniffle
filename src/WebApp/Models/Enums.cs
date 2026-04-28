namespace WebApp.Models;

public enum ErrorCode
{
    BadRequest = 400,
    NotFound = 404,
    InternalServerError = 500,

    //Unauthorized = 401,
    //Forbidden = 403,

    UnknownError = 0
}
