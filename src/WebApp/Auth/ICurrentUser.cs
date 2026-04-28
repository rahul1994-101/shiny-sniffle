using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace WebApp.Auth;

public interface ICurrentUser
{
    bool IsAuthenticated { get; }

    Guid? UserId { get; }

    string? Email { get; }

    string? FullName { get; }
}

public sealed class HttpContextCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal => httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    public Guid? UserId =>
        Guid.TryParse(Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    public string? Email => Principal?.FindFirstValue(ClaimTypes.Email);

    public string? FullName => Principal?.FindFirstValue(ClaimTypes.Name);
}
