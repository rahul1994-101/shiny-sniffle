using FluentValidation;

using WebApp.Features.Shared.Cqrs.Abstractions;

namespace WebApp.Features.Users.Commands;

public sealed record SignInRequest(string EmailId, string Password)
    : ICommand<AppResult<SignInResponse>>;

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

public sealed class SignInRequestHandler(UserRepository userRepo, SharedRepository sharedRepo)
    : IFeatureHandler<SignInRequest, AppResult<SignInResponse>>
{
    private readonly SharedRepository _sharedRepo = sharedRepo;

    public async Task<AppResult<SignInResponse>> HandleAsync(SignInRequest request, CancellationToken cancellationToken = default)
    {
        var result = new AppResult<SignInResponse>();

        #region # Execute

        var session = await userRepo.FindSessionByEmailAndPasswordAsync(request.EmailId, request.Password, cancellationToken);

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
}
