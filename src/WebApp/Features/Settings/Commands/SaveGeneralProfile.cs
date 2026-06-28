using FluentValidation;

using WebApp.Features._Shared.Abstractions;

namespace WebApp.Features.Settings.Commands;

public sealed record SaveGeneralProfileRequest(Guid UserId, string FirstName, string LastName)
    : ICommand<AppResult<SaveGeneralProfileResponse?>>;

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
    }
}

public sealed class SaveGeneralProfileRequestHandler(ISettingsRepository settings)
    : IFeatureHandler<SaveGeneralProfileRequest, AppResult<SaveGeneralProfileResponse?>>
{
    public async Task<AppResult<SaveGeneralProfileResponse?>> HandleAsync(
        SaveGeneralProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = new AppResult<SaveGeneralProfileResponse?>();

        #region # Execute

        var savedProfile = await settings.UpdateUserProfileAsync(
            request.UserId,
            request.FirstName,
            request.LastName,
            request.UserId);

        #endregion

        #region # Handle Result

        if (savedProfile is null)
        {
            result.Failure(ErrorCode.NotFound, "User not found.");
        }
        else
        {
            result.Success(ToResponse(savedProfile));
        }

        #endregion

        return result;
    }

    #region # Mapping

    private static SaveGeneralProfileResponse ToResponse(GeneralSettingsDto settings) => new()
    {
        Email = settings.Email,
        FirstName = settings.FirstName,
        LastName = settings.LastName
    };

    #endregion
}

public sealed class SaveGeneralProfileResponse
{
    public string Email { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
}
