namespace Application.Features.Dbo.EmailProviders.Commands;

using Application.Features.Dbo.EmailProviders;
using FluentValidation;

public sealed record DeleteEmailProviderRequest(Guid UserId, Guid ProviderId) : ICommand;

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
    : IRequestHandler<DeleteEmailProviderRequest>
{
    public async ValueTask<Result> HandleAsync(
        DeleteEmailProviderRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = new Result();

        #region # Execute

        var (found, blocked) = await emailProviderRepo.TrySoftDeleteAsync(
            request.ProviderId,
            request.UserId,
            cancellationToken);

        #endregion

        #region # Handle Result

        if (!found)
        {
            result.Failure(ErrorCode.NotFound, "Email provider not found.");
        }
        else if (blocked)
        {
            result.Failure(ErrorCode.BadRequest, "System providers cannot be deleted.");
        }
        else
        {
            result.Success();
        }

        #endregion

        return result;
    }
}
