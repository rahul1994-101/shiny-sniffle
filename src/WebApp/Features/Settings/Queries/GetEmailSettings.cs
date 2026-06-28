using FluentValidation;

using WebApp.Features._Shared.Abstractions;
using WebApp.Features.Settings;

namespace WebApp.Features.Settings.Queries;

public sealed record GetEmailSettingsRequest(Guid UserId)
    : IQuery<AppResult<EmailSettingsResponse?>>;

public sealed class GetEmailSettingsRequestValidator : AbstractValidator<GetEmailSettingsRequest>
{
    public GetEmailSettingsRequestValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User Id is required.");
    }
}

public sealed class GetEmailSettingsRequestHandler(ISettingsRepository settings)
    : IFeatureHandler<GetEmailSettingsRequest, AppResult<EmailSettingsResponse?>>
{
    public async Task<AppResult<EmailSettingsResponse?>> HandleAsync(
        GetEmailSettingsRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = new AppResult<EmailSettingsResponse?>();

        #region # Execute

        var emailSettings = await settings.GetUserEmailSettingsAsync(request.UserId, cancellationToken);

        #endregion

        #region # Handle Result

        result.Success(EmailSettingsMapping.FromEntity(emailSettings));

        #endregion

        return result;
    }
}
