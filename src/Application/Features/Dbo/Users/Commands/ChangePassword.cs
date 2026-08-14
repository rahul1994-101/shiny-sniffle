using FluentValidation;

namespace Application.Features.Dbo.Users.Commands;

public sealed record ChangePasswordRequest(Guid UserId, string CurrentPassword, string NewPassword, string ConfirmPassword)
    : ICommand;

public sealed class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User Id is required.");

        RuleFor(x => x.CurrentPassword)
            .NotEmpty()
            .WithMessage("Current password is required.");

        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .WithMessage("New password is required.")
            .Length(6, 255)
            .WithMessage("New password must be between 6 and 255 characters.");

        RuleFor(x => x.ConfirmPassword)
            .NotEmpty()
            .WithMessage("Confirm password is required.");

        RuleFor(x => x)
            .Must(request => string.Equals(request.NewPassword, request.ConfirmPassword, StringComparison.Ordinal))
            .WithMessage("New password and confirmation do not match.");
    }
}

public sealed class ChangePasswordRequestHandler(UserRepository userRepo)
    : IRequestHandler<ChangePasswordRequest>
{
    public async ValueTask<Result> HandleAsync(ChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        var result = new Result();

        #region # Execute

        var (updated, wrongPassword) = await userRepo.TryChangePasswordAsync(
            request.UserId,
            request.CurrentPassword,
            request.NewPassword,
            request.UserId,
            cancellationToken);

        #endregion

        #region # Handle Result

        if (wrongPassword)
        {
            result.Failure(ErrorCode.BadRequest, "Current password is incorrect.");
        }
        else if (!updated)
        {
            result.Failure(ErrorCode.NotFound, "User not found.");
        }
        else
        {
            result.Success();
        }

        #endregion

        return result;
    }
}
