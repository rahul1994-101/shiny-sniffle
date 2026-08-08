using Infrastructure.Persistence;
using Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
namespace Application.Features.Workspace.Contacts;

public sealed class ContactRepository(IDbContextFactory<AppDbContext> _dbContextFactory)
{
    public async Task<IReadOnlyList<ContactSummaryDto>> ListAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await using var ctx = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var rows = await ctx.Contacts
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.IsActive && !x.IsDeleted)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.DisplayName)
            .ToListAsync(cancellationToken);

        return rows.ConvertAll(ContactMapping.ToSummary);
    }

    public async Task<ContactDto?> GetByIdAsync(Guid userId, Guid contactId, CancellationToken cancellationToken = default)
    {
        await using var ctx = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var row = await FindActiveAsync(ctx, userId, contactId, asNoTracking: true, cancellationToken);
        return row is null ? null : ContactMapping.ToDto(row);
    }

    public async Task<(ContactDto? Saved, string? Error, bool NotFound)> SaveAsync(
        Guid userId,
        SaveContactDto dto,
        Guid updatedBy,
        CancellationToken cancellationToken = default)
    {
        await using var ctx = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var now = DateTime.UtcNow;
        var displayName = dto.DisplayName.Trim();
        var email = ContactMapping.NormalizeEmail(dto.Email);
        var phone = ContactMapping.NormalizePhone(dto.Phone);
        var notes = string.IsNullOrWhiteSpace(dto.Notes) ? null : dto.Notes.Trim();

        if (email is not null)
        {
            var emailTaken = await ctx.Contacts.AnyAsync(
                x =>
                    x.UserId == userId &&
                    !x.IsDeleted &&
                    x.Email == email &&
                    x.Id != dto.Id,
                cancellationToken);

            if (emailTaken)
            {
                return (null, "A contact with this email already exists.", false);
            }
        }

        Contact entity;

        if (dto.Id is { } id)
        {
            var existing = await FindActiveAsync(ctx, userId, id, asNoTracking: false, cancellationToken);
            if (existing is null)
            {
                return (null, null, true);
            }

            entity = existing;
            entity.DisplayName = displayName;
            entity.Email = email;
            entity.Phone = phone;
            entity.Notes = notes;
            entity.UpdatedBy = updatedBy;
            entity.UpdatedAt = now;
        }
        else
        {
            var sortOrder = await ctx.Contacts
                .Where(x => x.UserId == userId && !x.IsDeleted)
                .Select(x => (int?)x.SortOrder)
                .MaxAsync(cancellationToken) ?? 0;

            entity = new Contact
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                DisplayName = displayName,
                Email = email,
                Phone = phone,
                Notes = notes,
                Source = ContactSource.Manual,
                SortOrder = sortOrder + 10,
                CreatedBy = updatedBy,
                UpdatedBy = updatedBy,
                CreatedAt = now,
                UpdatedAt = now
            };
            await ctx.Contacts.AddAsync(entity, cancellationToken);
        }

        try
        {
            await ctx.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            return (null, ContactMapping.MapSaveError(ex), false);
        }

        return (ContactMapping.ToDto(entity), null, false);
    }

    public async Task<bool> SoftDeleteAsync(
        Guid userId,
        Guid contactId,
        Guid updatedBy,
        CancellationToken cancellationToken = default)
    {
        await using var ctx = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var entity = await FindActiveAsync(ctx, userId, contactId, asNoTracking: false, cancellationToken);
        if (entity is null)
        {
            return false;
        }

        entity.IsDeleted = true;
        entity.IsActive = false;
        entity.UpdatedBy = updatedBy;
        entity.UpdatedAt = DateTime.UtcNow;
        await ctx.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static async Task<Contact?> FindActiveAsync(
        AppDbContext ctx,
        Guid userId,
        Guid contactId,
        bool asNoTracking,
        CancellationToken cancellationToken)
    {
        var query = ctx.Contacts.Where(x =>
            x.Id == contactId &&
            x.UserId == userId &&
            x.IsActive &&
            !x.IsDeleted);

        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync(cancellationToken);
    }
}
