using FluentValidation;

using WebApp.Features._Shared.Abstractions;

namespace WebApp.Features.Settings.Queries;

public sealed record GetGeneralSettingsRequest(Guid UserId)
    : IQuery<AppResult<GetGeneralSettingsResponse?>>;

public sealed class GetGeneralSettingsRequestValidator : AbstractValidator<GetGeneralSettingsRequest>
{
    public GetGeneralSettingsRequestValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User Id is required.");
    }
}

public sealed class GetGeneralSettingsRequestHandler(ISettingsRepository settings)
    : IFeatureHandler<GetGeneralSettingsRequest, AppResult<GetGeneralSettingsResponse?>>
{
    public async Task<AppResult<GetGeneralSettingsResponse?>> HandleAsync(
        GetGeneralSettingsRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = new AppResult<GetGeneralSettingsResponse?>();

        #region # Execute

        var generalSettings = await settings.GetUserGeneralSettingsAsync(request.UserId);

        #endregion

        #region # Handle Result

        if (generalSettings is null)
        {
            result.Failure(ErrorCode.NotFound, "User not found.");
        }
        else
        {
            result.Success(ToResponse(generalSettings));
        }

        #endregion

        return result;
    }

    #region # Mapping

    private static GetGeneralSettingsResponse ToResponse(GeneralSettingsDto settings) => new()
    {
        Email = settings.Email,
        FirstName = settings.FirstName,
        LastName = settings.LastName
    };

    #endregion
}

public sealed class GetGeneralSettingsResponse
{
    public string Email { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
}
