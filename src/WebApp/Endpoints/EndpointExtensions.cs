namespace WebApp.Endpoints;

/// <summary>
/// Registers all minimal API endpoint groups. Add new <c>Map*Endpoints</c> calls here.
/// </summary>
public static class EndpointExtensions
{
    public static void MapAppEndpoints(this WebApplication app)
    {
        app.MapAuthEndpoints();
    }
}
