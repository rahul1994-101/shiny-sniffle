using Microsoft.EntityFrameworkCore;

using WebApp.Models;
using WebApp.Utilities.Helpers;

namespace WebApp.Data;

public sealed class Persistence(AppDbContext _ctx)
{
    public async Task<SignInResponse?> SignInAsync(SignInRequest signInRequest)
    {
        return new SignInResponse
        {
            Id = Guid.NewGuid(),
            Email = signInRequest.EmailId,
            FullName = "John Doe"
        };

        //var encPassword = signInRequest.Password.Encrypt();
        var encPassword = signInRequest.Password;

        return await _ctx.Users
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
    }
}
