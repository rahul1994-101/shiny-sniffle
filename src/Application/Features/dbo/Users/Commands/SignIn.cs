using FluentValidation;
using Application.Features.dbo.Users;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.dbo.Users.Commands;

public sealed record SignInRequest(string EmailId, string Password)
    : ICommand<SignInResponse>;

public sealed class SignInResponse : SessionDto
{
}

public sealed class SignInRequestValidator : AbstractValidator<SignInRequest>
{
    public SignInRequestValidator()
    {
        RuleFor(x => x.EmailId)
            .NotEmpty()
            .WithMessage("Email Id is required.")
            .Length(5, 100)
            .WithMessage("Email Id must be between 5 and 100 characters.");

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("Password is required.");
    }
}

public sealed class SignInRequestHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<SignInRequest, SignInResponse>
{
    public async ValueTask<Result<SignInResponse>> HandleAsync(SignInRequest request, CancellationToken cancellationToken = default)
    {
        var result = new Result<SignInResponse>();

        #region # Execute

        var session = await FindSessionByEmailAndPasswordAsync(request.EmailId, request.Password, cancellationToken);

        #endregion

        #region # Handle Result

        if (session is null)
        {
            result.Failure(ErrorCode.NotFound, "Invalid Credentials");
        }
        else
        {
            result.Success(session.AsResponse<SignInResponse>());
        }

        #endregion

        return result;
    }

    #region # Private Helpers

    private async Task<SessionDto?> FindSessionByEmailAndPasswordAsync(string emailId, string password, CancellationToken cancellationToken)
    {
        await using var ctx = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var user = await ctx.Users
            .AsNoTracking()
            .Where(x =>
                x.Email.ToLower() == emailId.ToLower() &&
                x.IsActive &&
                !x.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        if (user is null || !UserPasswordHelpers.MatchesStoredPassword(user.Password, password))
        {
            return null;
        }

        return SessionDto.FromEntity(user);
    }

    #endregion
}
