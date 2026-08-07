namespace Application.Features.EmailProviders.Queries;

using Application.Features.EmailProviders;
using FluentValidation;

public sealed record GetEmailProviderByIdRequest(Guid Id) : IQuery<GetEmailProviderByIdResponse>;

public sealed class GetEmailProviderByIdResponse : EmailProviderDto
{
}

public sealed class GetEmailProviderByIdRequestValidator : AbstractValidator<GetEmailProviderByIdRequest>
{
    public GetEmailProviderByIdRequestValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Provider Id is required.");
    }
}

public sealed class GetEmailProviderByIdRequestHandler(EmailProviderRepository emailProviderRepo)
    : IRequestHandler<GetEmailProviderByIdRequest, GetEmailProviderByIdResponse>
{
    public async ValueTask<Result<GetEmailProviderByIdResponse>> HandleAsync(
        GetEmailProviderByIdRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = new Result<GetEmailProviderByIdResponse>();

        #region # Execute

        var provider = await emailProviderRepo.GetByIdAsync(request.Id, cancellationToken);

        #endregion

        #region # Handle Result

        if (provider is null)
        {
            result.Failure(ErrorCode.NotFound, "Email provider not found.");
        }
        else
        {
            result.Success(provider.AsResponse<GetEmailProviderByIdResponse>());
        }

        #endregion

        return result;
    }
}
