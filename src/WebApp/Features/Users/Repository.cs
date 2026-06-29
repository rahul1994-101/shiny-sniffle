using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace WebApp.Features.Users;

public sealed class UserRepository(IDbContextFactory<AppDbContext> _dbContextFactory)
{
    public async Task<SessionDto?> FindSessionByEmailAndPasswordAsync(string emailId, string password, CancellationToken cancellationToken = default)
    {
        var encPassword = password;

        await using var ctx = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var user = await ctx.Users
            .AsNoTracking()
            .Where(x =>
                x.Email.ToLower() == emailId.ToLower() &&
                x.Password.ToLower() == encPassword.ToLower() &&
                x.IsActive &&
                !x.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        return user is null ? null : SessionDto.FromEntity(user);
    }
}
