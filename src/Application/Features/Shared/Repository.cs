using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Shared;

public sealed class SharedRepository(IDbContextFactory<AppDbContext> _dbContextFactory)
{
    public async Task<bool> ReturnTrueAsync(CancellationToken cancellationToken = default)
    {
        await using var _ = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return true;
    }
}
