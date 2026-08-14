namespace Application.Features.Dbo.EmailProviders.Queries;

using Application.Features.Dbo.EmailProviders;
using FluentValidation;

public sealed record GetAllEmailProvidersByUserIdRequest(Guid UserId) : IQuery<GetAllEmailProvidersByUserIdResponse>;

public sealed class GetAllEmailProvidersByUserIdResponse
{
    public IReadOnlyList<EmailProviderDto> Providers { get; init; } = [];
}

public sealed class GetAllEmailProvidersByUserIdRequestValidator : AbstractValidator<GetAllEmailProvidersByUserIdRequest>
{
    public GetAllEmailProvidersByUserIdRequestValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User Id is required.");
    }
}

public sealed class GetAllEmailProvidersByUserIdRequestHandler(EmailProviderRepository emailProviderRepo)
    : IRequestHandler<GetAllEmailProvidersByUserIdRequest, GetAllEmailProvidersByUserIdResponse>
{
    public async ValueTask<Result<GetAllEmailProvidersByUserIdResponse>> HandleAsync(
        GetAllEmailProvidersByUserIdRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = new Result<GetAllEmailProvidersByUserIdResponse>();

        #region # Execute

        var providers = await emailProviderRepo.GetAllEmailProvidersByUserIdAsync(request.UserId, cancellationToken);

        #endregion

        #region # Handle Result

        result.Success(new GetAllEmailProvidersByUserIdResponse { Providers = providers });

        #endregion

        return result;
    }
}
