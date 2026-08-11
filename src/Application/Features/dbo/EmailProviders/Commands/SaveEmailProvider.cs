namespace Application.Features.dbo.EmailProviders.Commands;

using Application.Features.dbo.EmailProviders;
using FluentValidation;

public sealed record SaveEmailProviderRequest(Guid UserId, SaveEmailProviderDto Provider)
    : ICommand<SaveEmailProviderResponse>;

public sealed class SaveEmailProviderResponse : EmailProviderDto
{
}

public sealed class SaveEmailProviderRequestValidator : AbstractValidator<SaveEmailProviderRequest>
{
    public SaveEmailProviderRequestValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User Id is required.");

        RuleFor(x => x.Provider)
            .NotNull()
            .WithMessage("Provider is required.");
    }
}

public sealed class SaveEmailProviderRequestHandler(EmailProviderRepository emailProviderRepo)
    : IRequestHandler<SaveEmailProviderRequest, SaveEmailProviderResponse>
{
    public async ValueTask<Result<SaveEmailProviderResponse>> HandleAsync(
        SaveEmailProviderRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = new Result<SaveEmailProviderResponse>();
        var dto = request.Provider;

        #region # Execute

        var validation = EmailProviderMapping.ValidateSave(dto);
        if (validation is not null)
        {
            result.Failure(ErrorCode.BadRequest, validation);
            return result;
        }

        var (saved, error, notFound, blockedSystem) = await emailProviderRepo.SaveAsync(
            dto,
            request.UserId,
            cancellationToken);

        #endregion

        #region # Handle Result

        if (notFound)
        {
            result.Failure(ErrorCode.NotFound, "Email provider not found.");
        }
        else if (blockedSystem)
        {
            result.Failure(ErrorCode.BadRequest, "System providers cannot be modified.");
        }
        else if (error is not null)
        {
            result.Failure(ErrorCode.BadRequest, error);
        }
        else if (saved is null)
        {
            result.Failure(ErrorCode.InternalServerError, "Failed to save email provider.");
        }
        else
        {
            result.Success(saved.AsResponse<SaveEmailProviderResponse>());
        }

        #endregion

        return result;
    }
}
