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

public sealed class GetEmailSettingsRequestHandler(UserSettingsRepository userSettingsRepo)
    : IRequestHandler<GetEmailSettingsRequest, GetEmailSettingsResponse>
{


    public async ValueTask<Result<GetEmailSettingsResponse>> HandleAsync(GetEmailSettingsRequest request, CancellationToken cancellationToken = default)
    {
        var result = new Result<GetEmailSettingsResponse>();

        #region # Execute

        var emailSettings = await userSettingsRepo.GetUserEmailSettingsAsync(request.UserId, cancellationToken);

        #endregion

        #region # Handle Result

        result.Success(EmailSettingsMapping.FromEntity(emailSettings).AsResponse<GetEmailSettingsResponse>());

        #endregion

        return result;
    }
}
