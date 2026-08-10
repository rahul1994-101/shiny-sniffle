using FluentValidation;

namespace Application.Features.dbo.UserSettings.Commands;

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

public sealed class ChangePasswordRequestHandler(UserSettingsRepository userSettingsRepo)
    : IRequestHandler<ChangePasswordRequest>
{
    public async ValueTask<Result> HandleAsync(ChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        var result = new Result();

        var currentMatches = await userSettingsRepo.UserPasswordMatchesAsync(request.UserId, request.CurrentPassword, cancellationToken);
        if (!currentMatches)
        {
            result.Failure(ErrorCode.BadRequest, "Current password is incorrect.");
            return result;
        }

        #region # Execute

        var updated = await userSettingsRepo.UpdateUserPasswordAsync(request.UserId, request.NewPassword, request.UserId, cancellationToken);

        #endregion

        #region # Handle Result

        if (!updated)
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
