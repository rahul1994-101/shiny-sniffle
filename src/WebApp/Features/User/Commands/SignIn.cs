using FluentValidation;

using WebApp.Features._Shared.Abstractions;

namespace WebApp.Features.User.Commands;

public sealed record SignInCommand(SignInRequest Request) : ICommand<AppResult<SignInResponse?>>;

public sealed class SignInCommandValidator : AbstractValidator<SignInCommand>
{
    public SignInCommandValidator()
    {
        RuleFor(x => x.Request)
            .NotNull()
            .WithMessage("Request can't be empty.");

        RuleFor(x => x.Request.EmailId)
            .NotEmpty()
            .WithMessage("Email Id is required.")
            .Length(5, 100)
            .WithMessage("Email Id must be between 5 and 100 characters.");

        RuleFor(x => x.Request.Password)
            .NotEmpty()
            .WithMessage("Password is required.");
    }
}

public sealed class SignInCommandHandler(IUserRepository users) : IFeatureHandler<SignInCommand, AppResult<SignInResponse?>>
{
    public async Task<AppResult<SignInResponse?>> HandleAsync(SignInCommand command, CancellationToken cancellationToken = default)
    {
        var result = new AppResult<SignInResponse?>();

        #region # Execute

        var user = await users.SignInAsync(command.Request);

        #endregion

        #region # Handle Result

        if (user is null)
        {
            result.Failure(ErrorCode.NotFound, "Invalid Credentials");
        }
        else
        {
            result.Success(user);
        }

        #endregion

        return result;
    }
}
