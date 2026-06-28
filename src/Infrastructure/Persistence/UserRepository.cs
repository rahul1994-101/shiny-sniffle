using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

public sealed class UserRepository(IDbContextFactory<AppDbContext> _dbContextFactory) : IUserRepository
{
    public async Task<User?> FindActiveByEmailAndPasswordAsync(
        string emailId,
        string password,
        CancellationToken cancellationToken = default)
    {
        //var encPassword = password.Encrypt();
        var encPassword = password;

        await using var ctx = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await ctx.Users
            .AsNoTracking()
            .Where(x =>
                x.Email.ToLower() == emailId.ToLower() &&
                x.Password.ToLower() == encPassword.ToLower() &&
                x.IsActive == true &&
                x.IsDeleted == false)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
