using Microsoft.EntityFrameworkCore;

using Core.DTOs;

namespace Infrastructure.Persistence;

public sealed class UserRepository(IDbContextFactory<AppDbContext> _dbContextFactory) : IUserRepository
{
    public async Task<SignInResponse?> SignInAsync(SignInRequest signInRequest)
    {
        //var encPassword = signInRequest.Password.Encrypt();
        var encPassword = signInRequest.Password;

        await using var ctx = await _dbContextFactory.CreateDbContextAsync();
        var user = await ctx.Users
            .AsNoTracking()
            .Where(x =>
                x.Email.ToLower() == signInRequest.EmailId.ToLower() &&
                x.Password.ToLower() == encPassword.ToLower() &&
                x.IsActive == true &&
                x.IsDeleted == false
            )
            .Select(x => new SignInResponse
            {
                Id = x.Id,
                Email = x.Email,
                FullName = x.FirstName + " " + x.LastName
            })
            .FirstOrDefaultAsync();

        return user;
    }
}
