using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Shared;

/// <summary>
/// Cross-slice data access only — queries spanning multiple feature folders.
/// AI/Services use slice repos for single-slice data; inject Shared when slices combine.
/// </summary>
public sealed class SharedRepository
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

    public SharedRepository(IDbContextFactory<AppDbContext> dbContextFactory) =>
        _dbContextFactory = dbContextFactory;
}
