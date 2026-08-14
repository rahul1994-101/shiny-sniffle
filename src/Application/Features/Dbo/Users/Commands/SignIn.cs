using FluentValidation;

namespace Application.Features.Dbo.Users.Commands;

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
            .Length(5, 255)
            .WithMessage("Email Id must be between 5 and 255 characters.");

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("Password is required.");
    }
}

public sealed class SignInRequestHandler(UserRepository userRepo)
    : IRequestHandler<SignInRequest, SignInResponse>
{
    public async ValueTask<Result<SignInResponse>> HandleAsync(SignInRequest request, CancellationToken cancellationToken = default)
    {
        var result = new Result<SignInResponse>();

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
