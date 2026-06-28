using FluentValidation;

using WebApp.Features._Shared.Abstractions;

namespace WebApp.Features.Settings.Queries;

public sealed record GetGeneralSettingsQuery(Guid UserId) : IQuery<AppResult<GeneralSettingsDto?>>;

public sealed class GetGeneralSettingsQueryValidator : AbstractValidator<GetGeneralSettingsQuery>
{
    public GetGeneralSettingsQueryValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User Id is required.");
    }
}

public sealed class GetGeneralSettingsQueryHandler(ISettingsRepository settings)
    : IFeatureHandler<GetGeneralSettingsQuery, AppResult<GeneralSettingsDto?>>
{
    public async Task<AppResult<GeneralSettingsDto?>> HandleAsync(
        GetGeneralSettingsQuery query,
        CancellationToken cancellationToken = default)
    {
        var result = new AppResult<GeneralSettingsDto?>();

        #region # Execute

        var generalSettings = await settings.GetUserGeneralSettingsAsync(query.UserId);

        #endregion

        #region # Handle Result

        if (generalSettings is null)
        {
            result.Failure(ErrorCode.NotFound, "User not found.");
        }
        else
        {
            result.Success(generalSettings);
        }

        #endregion

        return result;
    }
}
