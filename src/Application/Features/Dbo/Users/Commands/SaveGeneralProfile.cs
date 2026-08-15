using FluentValidation;

namespace Application.Features.Dbo.Users.Commands;

public sealed record SaveGeneralProfileRequest(
    Guid UserId,
    string FirstName,
    string LastName,
    string? Mobile,
    string Password
    )
    : ICommand<SaveGeneralProfileResponse>;

public sealed class SaveGeneralProfileResponse : GeneralSettingsDto
{
}

public sealed class SaveGeneralProfileRequestValidator : AbstractValidator<SaveGeneralProfileRequest>
{
    public SaveGeneralProfileRequestValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User Id is required.");

        RuleFor(x => x.FirstName)
            .NotEmpty()
            .WithMessage("First name is required.")
            .Length(2, 50)
            .WithMessage("First name must be between 2 and 50 characters.");

        RuleFor(x => x.LastName)
            .NotEmpty()
            .WithMessage("Last name is required.")
            .Length(2, 50)
            .WithMessage("Last name must be between 2 and 50 characters.");

        RuleFor(x => x.Mobile)
            .MaximumLength(20)
            .WithMessage("Mobile must be 20 characters or fewer.")
            .When(x => !string.IsNullOrWhiteSpace(x.Mobile));

        RuleFor(x => x.Password)
            .Length(6, 255)
            .WithMessage("Password must be between 6 and 255 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Password));
    }
}

public sealed class SaveGeneralProfileRequestHandler(UserRepository userRepo)
    : IRequestHandler<SaveGeneralProfileRequest, SaveGeneralProfileResponse>
{
    public async ValueTask<Result<SaveGeneralProfileResponse>> HandleAsync(SaveGeneralProfileRequest request, CancellationToken cancellationToken = default)
    {
        var result = new Result<SaveGeneralProfileResponse>();

        #region # Execute

        var profile = await userRepo.UpdateProfileAsync(
            request.UserId,
            request.FirstName,
            request.LastName,
            request.Mobile,
            request.Password,
            request.UserId,
            cancellationToken);

        #endregion

        #region # Handle Result

        if (profile is null)
        {
            result.Failure(ErrorCode.NotFound, "User not found.");
        }
        else
        {
            result.Success(profile.AsResponse<SaveGeneralProfileResponse>());
        }

        #endregion

        return result;
    }
}
