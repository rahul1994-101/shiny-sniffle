using Microsoft.EntityFrameworkCore;
using WebUI.Models;
using WebUI.Utilities.Helpers;

namespace WebUI.Data;

public sealed class Persistence(TenantDbContext _tenantDbContext)
{
    public async Task<IEnumerable<User>> GetUsers()
    {
        return await _tenantDbContext
            .Users
            .Where(x => !x.IsDeleted)
            .Select(x => new User 
            { 
                Id = x.Id,
                FirstName = x.FirstName, 
                LastName = x.LastName,
                Email = x.Email,
                Mobile = x.Mobile,
                Role = x.Role,
                IsActive = x.IsActive
            })
            .ToListAsync()
            .ConfigureAwait(false);
    }
}
