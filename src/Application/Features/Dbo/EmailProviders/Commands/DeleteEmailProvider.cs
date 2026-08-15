namespace Application.Features.Dbo.EmailProviders.Commands;

using Application.Features.Dbo.EmailProviders;
using FluentValidation;

public sealed record DeleteEmailProviderRequest(Guid UserId, Guid ProviderId) : ICommand<DeleteEmailProviderResponse>;

public sealed class DeleteEmailProviderResponse;

public sealed class DeleteEmailProviderRequestValidator : AbstractValidator<DeleteEmailProviderRequest>
{
    public DeleteEmailProviderRequestValidator()
    {
        RuleFor(x => x.ProviderId)
            .NotEmpty()
            .WithMessage("Provider Id is required.");

        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User Id is required.");
    }
}

public sealed class DeleteEmailProviderRequestHandler(EmailProviderRepository emailProviderRepo)
    : IRequestHandler<DeleteEmailProviderRequest, DeleteEmailProviderResponse>
{
    public async ValueTask<Result<DeleteEmailProviderResponse>> HandleAsync(
        DeleteEmailProviderRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = new Result<DeleteEmailProviderResponse>();

        #region # Execute

        var (found, blockedSystem, inUseByAccounts) = await emailProviderRepo.TrySoftDeleteAsync(
            request.ProviderId,
            request.UserId,
            cancellationToken);

        #endregion

        #region # Handle Result

        if (!found)
        {
            result.Failure(ErrorCode.NotFound, "Email provider not found.");
        }
        else if (blockedSystem)
        {
            result.Failure(ErrorCode.BadRequest, "System providers cannot be deleted.");
        }
        else if (inUseByAccounts)
        {
            result.Failure(
                ErrorCode.BadRequest,
                "This provider is used by one or more email accounts. Remove or reassign those accounts first.");
        }
        else
        {
            result.Success(new DeleteEmailProviderResponse());
        }

        #endregion

        return result;
    }
}
