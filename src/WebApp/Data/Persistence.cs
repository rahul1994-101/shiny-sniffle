using Microsoft.EntityFrameworkCore;
using WebApp.Models;
using WebApp.Utilities.Helpers;

namespace WebApp.Data;

public sealed class Persistence(AppDbContext _tenantDbContext)
{
    public async Task<IEnumerable<User>> GetUsers()
    {
        return await _tenantDbContext
            .Users
            .ToListAsync()
            .ConfigureAwait(false);
    }
}
