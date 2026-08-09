using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.EmailProviders;

public sealed class EmailProviderRepository(IDbContextFactory<AppDbContext> _dbContextFactory)
{
    public async Task<IReadOnlyList<EmailProviderDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        await using var ctx = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var rows = await ctx.EmailProviders
            .AsNoTracking()
            .Where(x => x.IsActive && !x.IsDeleted)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);

        return rows.ConvertAll(EmailProviderMapping.FromEntity);
    }

    public async Task<EmailProviderDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var ctx = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var entity = await ctx.EmailProviders
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.IsActive && !x.IsDeleted, cancellationToken);

        return entity is null ? null : EmailProviderMapping.FromEntity(entity);
    }

    public async Task<(EmailProviderDto? Saved, bool NotFound, bool BlockedSystem)> SaveAsync(
        SaveEmailProviderDto dto,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        await using var ctx = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var slug = dto.Slug.Trim().ToLowerInvariant();
        var now = DateTime.UtcNow;
        EmailProvider entity;

        if (dto.Id is { } id)
        {
            var existing = await ctx.EmailProviders.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);
            if (existing is null)
            {
                return (null, true, false);
            }

            if (existing.IsSystem)
            {
                return (null, false, true);
            }

            entity = existing;
            entity.Name = dto.Name.Trim();
            entity.Slug = slug;
            entity.ImapHost = dto.ImapHost.Trim();
            entity.ImapPort = dto.ImapPort;
            entity.ImapUseSsl = dto.ImapUseSsl;
            entity.SmtpHost = dto.SmtpHost.Trim();
            entity.SmtpPort = dto.SmtpPort;
            entity.SmtpUseSsl = dto.SmtpUseSsl;
            entity.SetupHelpUrl = string.IsNullOrWhiteSpace(dto.SetupHelpUrl) ? null : dto.SetupHelpUrl.Trim();
            entity.SortOrder = dto.SortOrder;
            entity.UpdatedBy = userId;
            entity.UpdatedAt = now;
        }
        else
        {
            entity = new EmailProvider
            {
                Name = dto.Name.Trim(),
                Slug = slug,
                ImapHost = dto.ImapHost.Trim(),
                ImapPort = dto.ImapPort,
                ImapUseSsl = dto.ImapUseSsl,
                SmtpHost = dto.SmtpHost.Trim(),
                SmtpPort = dto.SmtpPort,
                SmtpUseSsl = dto.SmtpUseSsl,
                SetupHelpUrl = string.IsNullOrWhiteSpace(dto.SetupHelpUrl) ? null : dto.SetupHelpUrl.Trim(),
                SortOrder = dto.SortOrder,
                IsSystem = false,
                CreatedBy = userId,
                UpdatedBy = userId,
                CreatedAt = now,
                UpdatedAt = now
            };
            await ctx.EmailProviders.AddAsync(entity, cancellationToken);
        }

        await ctx.SaveChangesAsync(cancellationToken);
        return (EmailProviderMapping.FromEntity(entity), false, false);
    }

    public async Task<(bool Found, bool Blocked)> TrySoftDeleteAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        await using var ctx = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var entity = await ctx.EmailProviders.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);
        if (entity is null)
        {
            return (false, false);
        }

        if (entity.IsSystem)
        {
            return (true, true);
        }

        entity.IsDeleted = true;
        entity.IsActive = false;
        entity.UpdatedBy = userId;
        entity.UpdatedAt = DateTime.UtcNow;
        await ctx.SaveChangesAsync(cancellationToken);
        return (true, false);
    }

    public async Task<bool> IsSlugTakenAsync(string slug, Guid? excludeId, CancellationToken cancellationToken = default)
    {
        var normalized = slug.Trim().ToLowerInvariant();
        await using var ctx = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await ctx.EmailProviders.AnyAsync(
            x => x.Slug == normalized && !x.IsDeleted && x.Id != excludeId,
            cancellationToken);
    }
}
