using FluentValidation;

using WebApp.Features._Shared.Abstractions;

namespace WebApp.Features.User.Commands;

public sealed record SignInRequest(string EmailId, string Password)
    : ICommand<AppResult<SignInResponse>>;

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

public sealed class SignInRequestHandler(IUserRepository users)
    : IFeatureHandler<SignInRequest, AppResult<SignInResponse>>
{
    public async Task<AppResult<SignInResponse>> HandleAsync(
        SignInRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = new AppResult<SignInResponse>();

        #region # Execute

        var user = await users.FindActiveByEmailAndPasswordAsync(
            request.EmailId,
            request.Password,
            cancellationToken);

        #endregion

        #region # Handle Result

        if (user is null)
        {
            result.Failure(ErrorCode.NotFound, "Invalid Credentials");
        }
        else
        {
            result.Success(ToResponse(user));
        }

        #endregion

        return result;
    }

    #region # Mapping

    private static SignInResponse ToResponse(Core.Entities.User user) => new()
    {
        Id = user.Id,
        Email = user.Email,
        FullName = $"{user.FirstName} {user.LastName}".Trim()
    };

    #endregion
}

public sealed class SignInResponse
{
    public Guid Id { get; init; }
    public string Email { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
}
