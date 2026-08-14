using Application.Utilities.Extensions;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Dbo.Users;

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

        if (user is null || !UserPasswordHelpers.MatchesStoredPassword(user.Password, password))
        {
            return null;
        }

        return SessionDto.FromEntity(user);
    }

    public async Task<GeneralSettingsDto?> GetGeneralSettingsByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await using var ctx = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var user = await ctx.Users
            .AsNoTracking()
            .Where(x =>
                x.Id == userId &&
                x.IsActive &&
                !x.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        return user is null ? null : GeneralSettingsDto.FromEntity(user);
    }

    public async Task<GeneralSettingsDto?> UpdateProfileAsync(Guid userId, string firstName, string lastName, Guid updatedBy, CancellationToken cancellationToken = default)
    {
        await using var ctx = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var user = await FindActiveTrackedUserAsync(ctx, userId, cancellationToken);
        if (user is null)
        {
            return null;
        }

        user.FirstName = firstName.Trim();
        user.LastName = lastName.Trim();
        user.UpdatedBy = updatedBy;
        user.UpdatedAt = DateTime.UtcNow;
        await ctx.SaveChangesAsync(cancellationToken);
        return GeneralSettingsDto.FromEntity(user);
    }

    public async Task<(bool Updated, bool WrongPassword)> TryChangePasswordAsync(
        Guid userId,
        string currentPassword,
        string newPassword,
        Guid updatedBy,
        CancellationToken cancellationToken = default)
    {
        await using var ctx = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var user = await FindActiveTrackedUserAsync(ctx, userId, cancellationToken);
        if (user is null)
        {
            return (false, false);
        }

        if (!UserPasswordHelpers.MatchesStoredPassword(user.Password, currentPassword))
        {
            return (false, true);
        }

        user.Password = newPassword.Trim().Encrypt();
        user.UpdatedBy = updatedBy;
        user.UpdatedAt = DateTime.UtcNow;
        await ctx.SaveChangesAsync(cancellationToken);
        return (true, false);
    }

    private static Task<User?> FindActiveTrackedUserAsync(AppDbContext ctx, Guid userId, CancellationToken cancellationToken) =>
        ctx.Users
            .Where(x =>
                x.Id == userId &&
                x.IsActive &&
                !x.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);
}
