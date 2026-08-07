using Application.Features.EmailAccounts;
using Application.Features.EmailProviders;
using FluentValidation;

namespace Application.Features.UserSettings.Queries;

public sealed record GetEmailSettingsRequest(Guid UserId)
    : IQuery<GetEmailSettingsResponse>;

public sealed class GetEmailSettingsResponse : EmailSettingsDto
{
}

public sealed class GetEmailSettingsRequestValidator : AbstractValidator<GetEmailSettingsRequest>
{
    public GetEmailSettingsRequestValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User Id is required.");
    }
}

public sealed class GetEmailSettingsRequestHandler(
    EmailAccountRepository emailAccountRepo,
    EmailProviderRepository emailProviderRepo)
    : IRequestHandler<GetEmailSettingsRequest, GetEmailSettingsResponse>
{


    public async ValueTask<Result<GetEmailSettingsResponse>> HandleAsync(GetEmailSettingsRequest request, CancellationToken cancellationToken = default)
    {
        var result = new Result<GetEmailSettingsResponse>();

        #region # Execute

        var emailSettings = await emailAccountRepo.GetDefaultEmailSettingsAsync(request.UserId, cancellationToken);
        var dto = EmailSettingsMapping.FromEntity(emailSettings);

        var catalog = await emailProviderRepo.ListAsync(cancellationToken);
        if (catalog.Count > 0)
        {
            _ = EmailSettingsCatalog.TryApplyCatalog(dto, catalog);
        }

        #endregion

        #region # Handle Result

        result.Success(dto.AsResponse<GetEmailSettingsResponse>());

        #endregion

        return result;
    }
}
