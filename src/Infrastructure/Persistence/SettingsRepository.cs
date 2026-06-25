using Microsoft.EntityFrameworkCore;

using Core.DTOs;
using Core.Entities;

namespace Infrastructure.Persistence;

public sealed class SettingsRepository(IDbContextFactory<AppDbContext> _dbContextFactory) : ISettingsRepository
{
    public async Task<GeneralSettingsDto?> GetUserGeneralSettingsAsync(Guid userId)
    {
        await using var ctx = await _dbContextFactory.CreateDbContextAsync();
        return await ctx.Users
            .AsNoTracking()
            .Where(x =>
                x.Id == userId &&
                x.IsActive == true &&
                x.IsDeleted == false)
            .Select(x => new GeneralSettingsDto
            {
                FirstName = x.FirstName,
                LastName = x.LastName,
                Email = x.Email
            })
            .FirstOrDefaultAsync();
    }

    public async Task<GeneralSettingsDto?> UpdateUserProfileAsync(Guid userId, string firstName, string lastName, Guid updatedBy)
    {
        await using var ctx = await _dbContextFactory.CreateDbContextAsync();
        var user = await ctx.Users
            .FirstOrDefaultAsync(x =>
                x.Id == userId &&
                x.IsActive == true &&
                x.IsDeleted == false);

        if (user is null)
        {
            return null;
        }

        user.FirstName = firstName.Trim();
        user.LastName = lastName.Trim();
        user.UpdatedBy = updatedBy;
        user.UpdatedAt = DateTime.UtcNow;

        await ctx.SaveChangesAsync();

        return new GeneralSettingsDto
        {
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email
        };
    }

    public async Task<bool> UserPasswordMatchesAsync(Guid userId, string password)
    {
        await using var ctx = await _dbContextFactory.CreateDbContextAsync();
        return await ctx.Users
            .AsNoTracking()
            .AnyAsync(x =>
                x.Id == userId &&
                x.IsActive == true &&
                x.IsDeleted == false &&
                x.Password.ToLower() == password.ToLower());
    }

    public async Task<bool> UpdateUserPasswordAsync(Guid userId, string newPassword, Guid updatedBy)
    {
        await using var ctx = await _dbContextFactory.CreateDbContextAsync();
        var user = await ctx.Users
            .FirstOrDefaultAsync(x =>
                x.Id == userId &&
                x.IsActive == true &&
                x.IsDeleted == false);

        if (user is null)
        {
            return false;
        }

        user.Password = newPassword;
        user.UpdatedBy = updatedBy;
        user.UpdatedAt = DateTime.UtcNow;

        await ctx.SaveChangesAsync();
        return true;
    }

    public async Task<EmailSettings?> GetUserEmailSettingsAsync(Guid userId)
    {
        await using var ctx = await _dbContextFactory.CreateDbContextAsync();
        var emailSettingsJson = await ctx.UserSettings
            .AsNoTracking()
            .Where(x =>
                x.UserId == userId &&
                x.IsActive == true &&
                x.IsDeleted == false)
            .Select(x => x.EmailSettingsJson)
            .FirstOrDefaultAsync();

        return EmailSettingsJson.FromJson(emailSettingsJson);
    }

    public async Task<EmailSettings?> SaveUserEmailSettingsAsync(Guid userId, EmailSettings? emailSettings, Guid updatedBy)
    {
        await using var ctx = await _dbContextFactory.CreateDbContextAsync();
        var existing = await ctx.UserSettings
            .Where(x =>
                x.UserId == userId &&
                x.IsActive == true &&
                x.IsDeleted == false)
            .FirstOrDefaultAsync();

        var now = DateTime.UtcNow;
        var emailSettingsJson = EmailSettingsJson.ToJson(emailSettings);

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

            await ctx.UserSettings.AddAsync(entity);
            await ctx.SaveChangesAsync();
            return EmailSettingsJson.FromJson(entity.EmailSettingsJson);
        }

        existing.EmailSettingsJson = emailSettingsJson;
        existing.UpdatedBy = updatedBy;
        existing.UpdatedAt = now;

        await ctx.SaveChangesAsync();
        return EmailSettingsJson.FromJson(existing.EmailSettingsJson);
    }
}
