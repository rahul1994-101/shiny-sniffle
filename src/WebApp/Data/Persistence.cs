using Microsoft.EntityFrameworkCore;
using WebApp.Models;
using WebApp.Utilities.Extensions;
using WebApp.Utilities.Helpers;

namespace WebApp.Data;

public sealed class Persistence(AppDbContext _ctx)
{
    public async Task<User?> SignInAsync(SignInRequest signInRequest)
    {
        //var encPassword = signInRequest.Password.Encrypt();
        var encPassword = signInRequest.Password;

        return await _ctx.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.Email.ToLower() == signInRequest.EmailId.ToLower() &&
                x.Password.ToLower() == encPassword &&
                x.IsActive == true
            );
    }
}
