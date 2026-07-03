using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using WebApp.Utilities.Extensions;

namespace WebApp.Features.Users;

public sealed class UserRepository(IDbContextFactory<AppDbContext> _dbContextFactory)
{
    public async Task<SessionDto?> FindSessionByEmailAndPasswordAsync(string emailId, string password, CancellationToken cancellationToken = default)
    {
        await using var ctx = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var user = await ctx.Users
            .AsNoTracking()
            .Where(x =>
                x.Email.ToLower() == emailId.ToLower() &&
                x.IsActive &&
                !x.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        if (user is null || !user.Password.MatchesStoredPassword(password))
        {
            return null;
        }

        return SessionDto.FromEntity(user);
    }
}
