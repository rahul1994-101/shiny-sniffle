namespace WebApp.Models;

/// <summary>Settings page tab routes (<c>/settings/general</c>, <c>/settings/email</c>).</summary>
public enum SettingsSection
{
    General = 0,
    Email = 1
}

public enum ErrorCode
{
    BadRequest = 400,
    NotFound = 404,
    InternalServerError = 500,

    //Unauthorized = 401,
    //Forbidden = 403,

    UnknownError = 0
}
