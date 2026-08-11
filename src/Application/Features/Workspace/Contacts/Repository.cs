using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.workspace.Contacts;

public sealed class ContactRepository(
    IDbContextFactory<AppDbContext> _dbContextFactory,
    SharedRepository _sharedRepo)
{
    public async Task<IReadOnlyList<ContactSummaryDto>> ListAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await using var ctx = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var rows = await ctx.Contacts
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.IsActive && !x.IsDeleted)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.FirstName)
            .ThenBy(x => x.LastName)
            .ToListAsync(cancellationToken);

        var ids = rows.ConvertAll(x => x.Id);
        var taxonomy = await _sharedRepo.LoadTaxonomyForReferablesAsync(
            ctx,
            userId,
            ReferableKind.Contact,
            ids,
            cancellationToken);

        return rows.ConvertAll(entity =>
        {
            taxonomy.TryGetValue(entity.Id, out var tax);
            return ContactMapping.ToSummary(entity, tax);
        });
    }

    public async Task<ContactDto?> GetByIdAsync(Guid userId, Guid contactId, CancellationToken cancellationToken = default)
    {
        await using var ctx = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var row = await FindActiveAsync(ctx, userId, contactId, asNoTracking: true, cancellationToken);
        if (row is null)
        {
            return null;
        }

        var taxonomy = await _sharedRepo.LoadTaxonomyForReferablesAsync(
            ctx,
            userId,
            ReferableKind.Contact,
            [row.Id],
            cancellationToken);
        taxonomy.TryGetValue(row.Id, out var tax);
        return ContactMapping.ToDto(row, tax);
    }

    public async Task<(ContactDto? Saved, string? Error, bool NotFound)> SaveAsync(
        Guid userId,
        SaveContactDto dto,
        Guid updatedBy,
        CancellationToken cancellationToken = default)
    {
        await using var ctx = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var now = DateTime.UtcNow;
        var firstName = dto.FirstName.Trim();
        var lastName = dto.LastName.Trim();
        var resolvedAlias = await ResolveAliasAsync(
            ctx, userId, dto.Id, ContactMapping.NormalizeAlias(dto.Alias), firstName, lastName, cancellationToken);
        var email = ContactMapping.NormalizeEmail(dto.Email);
        var phone = ContactMapping.NormalizePhone(dto.Phone);
        var context = string.IsNullOrWhiteSpace(dto.Context) ? null : dto.Context.Trim();

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

        var aliasTaken = await ctx.Contacts.AnyAsync(
            x =>
                x.UserId == userId &&
                !x.IsDeleted &&
                x.Alias == resolvedAlias &&
                x.Id != dto.Id,
            cancellationToken);

        if (aliasTaken)
        {
            return (null, "A contact with this alias already exists.", false);
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
            entity.FirstName = firstName;
            entity.LastName = lastName;
            entity.Alias = resolvedAlias;
            entity.Email = email;
            entity.Phone = phone;
            entity.Context = context;
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
                FirstName = firstName,
                LastName = lastName,
                Alias = resolvedAlias,
                Email = email,
                Phone = phone,
                Context = context,
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

        var (syncOk, syncError) = await _sharedRepo.SyncTaxonomyAsync(
            ctx,
            userId,
            ReferableKind.Contact,
            entity.Id,
            dto.TagIds.ToList(),
            dto.BucketIds.ToList(),
            cancellationToken);

        if (!syncOk)
        {
            return (null, syncError, false);
        }

        await ctx.SaveChangesAsync(cancellationToken);

        var taxonomy = await _sharedRepo.LoadTaxonomyForReferablesAsync(
            ctx,
            userId,
            ReferableKind.Contact,
            [entity.Id],
            cancellationToken);
        taxonomy.TryGetValue(entity.Id, out var tax);
        return (ContactMapping.ToDto(entity, tax), null, false);
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

        await _sharedRepo.RemoveTaxonomyForReferableAsync(ctx, userId, ReferableKind.Contact, contactId, cancellationToken);
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

    private static async Task<string> ResolveAliasAsync(
        AppDbContext ctx,
        Guid userId,
        Guid? excludeContactId,
        string? normalizedAlias,
        string firstName,
        string lastName,
        CancellationToken cancellationToken)
    {
        if (normalizedAlias is not null)
        {
            return normalizedAlias;
        }

        var stem = ContactMapping.BuildAliasStem(firstName, lastName);
        for (var index = 1; index < 10_000; index++)
        {
            var candidate = ContactMapping.AliasWithNumericSuffix(stem, index);
            var taken = await ctx.Contacts.AnyAsync(
                x =>
                    x.UserId == userId &&
                    !x.IsDeleted &&
                    x.Alias == candidate &&
                    x.Id != excludeContactId,
                cancellationToken);

            if (!taken)
            {
                return candidate;
            }
        }

        var fallback = ContactMapping.AliasWithNumericSuffix(
            stem,
            Random.Shared.Next(1000, 9999));
        return fallback;
    }
}
