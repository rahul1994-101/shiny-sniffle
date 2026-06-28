using FluentValidation;

using WebApp.Features._Shared.Abstractions;
using WebApp.Features.Settings;

namespace WebApp.Features.Settings.Queries;

public sealed record GetGeneralSettingsRequest(Guid UserId)
    : IQuery<AppResult<GeneralSettingsResponse?>>;

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
    : IFeatureHandler<GetGeneralSettingsRequest, AppResult<GeneralSettingsResponse?>>
{
    public async Task<AppResult<GeneralSettingsResponse?>> HandleAsync(
        GetGeneralSettingsRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = new AppResult<GeneralSettingsResponse?>();

        #region # Execute

        var user = await settings.GetActiveUserAsync(request.UserId, cancellationToken);

        #endregion

        #region # Handle Result

        if (user is null)
        {
            result.Failure(ErrorCode.NotFound, "User not found.");
        }
        else
        {
            result.Success(GeneralSettingsResponse.FromEntity(user));
        }

        #endregion

        return result;
    }
}
