using Infrastructure.Persistence;
using Infrastructure.Utilities.Helpers;
using Microsoft.EntityFrameworkCore;
using WebApp.Utilities.Extensions;

namespace WebApp.Features.UserSettings;

public sealed class UserSettingsRepository(IDbContextFactory<AppDbContext> _dbContextFactory)
{
    public async Task<GeneralSettingsDto?> GetGeneralSettingsAsync(Guid userId, CancellationToken cancellationToken = default)
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

    public async Task<GeneralSettingsDto?> UpdateUserProfileAsync(Guid userId, string firstName, string lastName, Guid updatedBy, CancellationToken cancellationToken = default)
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

    public async Task<bool> UserPasswordMatchesAsync(Guid userId, string password, CancellationToken cancellationToken = default)
    {
        await using var ctx = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var storedPassword = await ctx.Users
            .AsNoTracking()
            .Where(x =>
                x.Id == userId &&
                x.IsActive &&
                !x.IsDeleted)
            .Select(x => x.Password)
            .FirstOrDefaultAsync(cancellationToken);

        return storedPassword is not null && storedPassword.MatchesStoredPassword(password);
    }

    public async Task<bool> UpdateUserPasswordAsync(Guid userId, string newPassword, Guid updatedBy, CancellationToken cancellationToken = default)
    {
        await using var ctx = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var user = await FindActiveTrackedUserAsync(ctx, userId, cancellationToken);
        if (user is null)
        {
            return false;
        }

        user.Password = newPassword.Trim().Encrypt();
        user.UpdatedBy = updatedBy;
        user.UpdatedAt = DateTime.UtcNow;
        await ctx.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<EmailSettings?> GetUserEmailSettingsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await using var ctx = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var emailSettingsJson = await ctx.UserSettings
            .AsNoTracking()
            .Where(x =>
                x.UserId == userId &&
                x.IsActive &&
                !x.IsDeleted)
            .Select(x => x.EmailSettingsJson)
            .FirstOrDefaultAsync(cancellationToken);

        return EmailSettingsJsonHelpers.FromJson(emailSettingsJson);
    }

    public async Task<EmailSettings?> SaveUserEmailSettingsAsync(Guid userId, EmailSettings? emailSettings, Guid updatedBy, CancellationToken cancellationToken = default)
    {
        await using var ctx = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var existing = await ctx.UserSettings
            .Where(x =>
                x.UserId == userId &&
                x.IsActive &&
                !x.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        var now = DateTime.UtcNow;
        var emailSettingsJson = EmailSettingsJsonHelpers.ToJson(emailSettings);

        if (existing is null)
        {
            var entity = new UserSetting
            {
                UserId = userId,
                EmailSettingsJson = emailSettingsJson,
                CreatedBy = updatedBy,
                UpdatedBy = updatedBy,
                CreatedAt = now,
                UpdatedAt = now
            };

            await ctx.UserSettings.AddAsync(entity, cancellationToken);
            await ctx.SaveChangesAsync(cancellationToken);
            return EmailSettingsJsonHelpers.FromJson(entity.EmailSettingsJson);
        }

        existing.EmailSettingsJson = emailSettingsJson;
        existing.UpdatedBy = updatedBy;
        existing.UpdatedAt = now;
        await ctx.SaveChangesAsync(cancellationToken);
        return EmailSettingsJsonHelpers.FromJson(existing.EmailSettingsJson);
    }

    private static Task<User?> FindActiveTrackedUserAsync(AppDbContext ctx, Guid userId, CancellationToken cancellationToken) =>
        ctx.Users
            .Where(x =>
                x.Id == userId &&
                x.IsActive &&
                !x.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);
}
