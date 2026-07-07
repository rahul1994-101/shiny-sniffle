using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Users;

public sealed class UserRepository
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

    public UserRepository(IDbContextFactory<AppDbContext> dbContextFactory) =>
        _dbContextFactory = dbContextFactory;

    // Promoted methods added here when 2+ consumers need the same data access.
}
