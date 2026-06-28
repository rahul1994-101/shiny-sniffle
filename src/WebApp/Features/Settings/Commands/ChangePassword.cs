using FluentValidation;

using WebApp.Features._Shared.Abstractions;

namespace WebApp.Features.Settings.Commands;

public sealed record ChangePasswordCommand(ChangePasswordRequest Request) : ICommand<AppResult>;

public sealed class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(x => x.Request)
            .NotNull()
            .WithMessage("Request can't be empty.");

        RuleFor(x => x.Request.UserId)
            .NotEmpty()
            .WithMessage("User Id is required.");

        RuleFor(x => x.Request.CurrentPassword)
            .NotEmpty()
            .WithMessage("Current password is required.");

        RuleFor(x => x.Request.NewPassword)
            .NotEmpty()
            .WithMessage("New password is required.")
            .Length(6, 255)
            .WithMessage("New password must be between 6 and 255 characters.");

        RuleFor(x => x.Request.ConfirmPassword)
            .NotEmpty()
            .WithMessage("Confirm password is required.");

        RuleFor(x => x.Request)
            .Must(request => string.Equals(request.NewPassword, request.ConfirmPassword, StringComparison.Ordinal))
            .WithMessage("New password and confirmation do not match.");
    }
}

public sealed class ChangePasswordCommandHandler(ISettingsRepository settings)
    : IFeatureHandler<ChangePasswordCommand, AppResult>
{
    public async Task<AppResult> HandleAsync(ChangePasswordCommand command, CancellationToken cancellationToken = default)
    {
        var result = new AppResult();
        var request = command.Request;

        var currentMatches = await settings.UserPasswordMatchesAsync(request.UserId, request.CurrentPassword);
        if (!currentMatches)
        {
            result.Failure(ErrorCode.BadRequest, "Current password is incorrect.");
            return result;
        }

        #region # Execute

        var updated = await settings.UpdateUserPasswordAsync(
            request.UserId,
            request.NewPassword,
            request.UserId);

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
