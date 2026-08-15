using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Dbo.EmailProviders;

public sealed class EmailProviderRepository(IDbContextFactory<AppDbContext> _dbContextFactory)
{
    public async Task<IReadOnlyList<EmailProviderDto>> GetAllEmailProvidersByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await using var ctx = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var rows = await ctx.EmailProviders
            .AsNoTracking()
            .Where(x => x.IsActive && !x.IsDeleted && (x.IsSystem || x.UserId == userId))
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);

        return rows.ConvertAll(EmailProviderDto.FromEntity);
    }

    public async Task<EmailProviderDto?> GetEmailProviderByIdAsync(Guid userId, Guid providerId, CancellationToken cancellationToken = default)
    {
        await using var ctx = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var entity = await ctx.EmailProviders
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.Id == providerId && x.IsActive && !x.IsDeleted && (x.IsSystem || x.UserId == userId),
                cancellationToken);

        return entity is null ? null : EmailProviderDto.FromEntity(entity);
    }

    public async Task<EmailProviderDto?> GetEmailProviderBySlugAsync(
        Guid userId,
        string slug,
        CancellationToken cancellationToken = default)
    {
        var normalized = EmailProviderCatalog.NormalizeSlug(slug);
        await using var ctx = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var entity = await ctx.EmailProviders
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.Slug == normalized && x.IsActive && !x.IsDeleted && (x.IsSystem || x.UserId == userId),
                cancellationToken);

        return entity is null ? null : EmailProviderDto.FromEntity(entity);
    }

    public async Task<(EmailProviderDto? Saved, string? Error, bool NotFound, bool BlockedSystem)> SaveAsync(
        SaveEmailProviderDto dto,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        await using var ctx = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var name = dto.Name.Trim();
        var now = DateTime.UtcNow;

        var slug = await EmailProviderMapping.ResolveSlugAsync(
            (candidate, excludeId, ct) => IsSlugTakenAsync(ctx, userId, candidate, excludeId, ct),
            name,
            dto.Slug,
            dto.Id,
            cancellationToken);

        if (await IsSlugTakenAsync(ctx, userId, slug, dto.Id, cancellationToken))
        {
            return (null, "Slug is already in use.", false, false);
        }

        EmailProvider entity;

        if (dto.Id is { } id)
        {
            var existing = await ctx.EmailProviders.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);
            if (existing is null || (!existing.IsSystem && existing.UserId != userId))
            {
                return (null, null, true, false);
            }

            if (existing.IsSystem)
            {
                return (null, null, false, true);
            }

            entity = existing;
            entity.Name = name;
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
                UserId = userId,
                Name = name,
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
        return (EmailProviderDto.FromEntity(entity), null, false, false);
    }

    public async Task<(bool Found, bool Blocked)> TrySoftDeleteAsync(Guid providerId, Guid userId, CancellationToken cancellationToken = default)
    {
        await using var ctx = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var entity = await ctx.EmailProviders.FirstOrDefaultAsync(x => x.Id == providerId && !x.IsDeleted, cancellationToken);
        if (entity is null || (!entity.IsSystem && entity.UserId != userId))
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

    private static Task<bool> IsSlugTakenAsync(
        AppDbContext ctx,
        Guid userId,
        string slug,
        Guid? excludeId,
        CancellationToken cancellationToken) =>
        ctx.EmailProviders.AnyAsync(
            x => x.Slug == slug && !x.IsDeleted && x.Id != excludeId && (x.IsSystem || x.UserId == userId),
            cancellationToken);
}
