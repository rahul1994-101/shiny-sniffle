namespace WebApp.Utilities.Helpers;

// Placeholder until auth is wired up. Replace this with a value
// derived from the authenticated user (e.g. SignInResponse.Id).
public static class CurrentUser
{
    public static Guid Id { get; } = Guid.Parse("00000000-0000-0000-0000-000000000001");
}
