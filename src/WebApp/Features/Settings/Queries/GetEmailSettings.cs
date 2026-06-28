using FluentValidation;

using WebApp.Features._Shared.Abstractions;
using WebApp.Utilities.Helpers;

namespace WebApp.Features.Settings.Queries;

public sealed record GetEmailSettingsQuery(Guid UserId) : IQuery<AppResult<EmailSettingsDto?>>;

public sealed class GetEmailSettingsQueryValidator : AbstractValidator<GetEmailSettingsQuery>
{
    public GetEmailSettingsQueryValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User Id is required.");
    }
}

public sealed class GetEmailSettingsQueryHandler(ISettingsRepository settings)
    : IFeatureHandler<GetEmailSettingsQuery, AppResult<EmailSettingsDto?>>
{
    public async Task<AppResult<EmailSettingsDto?>> HandleAsync(
        GetEmailSettingsQuery query,
        CancellationToken cancellationToken = default)
    {
        var result = new AppResult<EmailSettingsDto?>();

        #region # Execute

        var emailSettings = await settings.GetUserEmailSettingsAsync(query.UserId);

        #endregion

        #region # Handle Result

        result.Success(EmailSettingsHelpers.ToDto(emailSettings));

        #endregion

        return result;
    }
}
