using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace WebApp.Data;

/// <summary>
/// Persists mock sign-in email in the browser (Blazor <see cref="ProtectedLocalStorage"/>).
/// </summary>
public sealed class MockSessionPersistence
{
    private readonly ProtectedLocalStorage _storage;
    private readonly Repository _repo;

    public MockSessionPersistence(ProtectedLocalStorage storage, Repository repo)
    {
        _storage = storage;
        _repo = repo;
    }

    public async Task TryRestoreAsync()
    {
        if (_repo.HasMockSession)
        {
            return;
        }

        try
        {
            var result = await _storage.GetAsync<string>(MockSessionStorageKeys.UserEmail);
            if (result.Success && !string.IsNullOrWhiteSpace(result.Value))
            {
                _repo.StartMockSession(result.Value.Trim(), null);
            }
        }
        catch (InvalidOperationException)
        {
            // No JS context (prerender).
        }
    }

    public async Task SaveEmailAsync(string email)
    {
        await _storage.SetAsync(MockSessionStorageKeys.UserEmail, email.Trim());
    }

    public async Task ClearAsync()
    {
        try
        {
            await _storage.DeleteAsync(MockSessionStorageKeys.UserEmail);
        }
        catch (InvalidOperationException)
        {
        }
    }
}
