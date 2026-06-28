using FluentValidation;

using WebApp.Features._Shared.Abstractions;
using WebApp.Utilities.Helpers;

namespace WebApp.Features.Settings.Queries;

public sealed record GetEmailSettingsRequest(Guid UserId)
    : IQuery<AppResult<GetEmailSettingsResponse?>>;

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
    : IFeatureHandler<GetEmailSettingsRequest, AppResult<GetEmailSettingsResponse?>>
{
    public async Task<AppResult<GetEmailSettingsResponse?>> HandleAsync(
        GetEmailSettingsRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = new AppResult<GetEmailSettingsResponse?>();

        #region # Execute

        var emailSettings = await settings.GetUserEmailSettingsAsync(request.UserId);

        #endregion

        #region # Handle Result

        var dto = EmailSettingsHelpers.ToDto(emailSettings);
        if (dto is null)
        {
            result.Failure(ErrorCode.InternalServerError, "Failed to load email settings.");
        }
        else
        {
            result.Success(new GetEmailSettingsResponse { Email = dto });
        }

        #endregion

        return result;
    }
}

public sealed class GetEmailSettingsResponse
{
    public EmailSettingsDto Email { get; init; } = null!;
}
