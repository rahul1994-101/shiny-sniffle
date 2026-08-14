namespace Application.Features.Dbo.EmailProviders.Queries;

using Application.Features.Dbo.EmailProviders;
using FluentValidation;

public sealed record ListEmailProvidersRequest(Guid UserId) : IQuery<ListEmailProvidersResponse>;

public sealed class ListEmailProvidersResponse
{
    public IReadOnlyList<EmailProviderDto> Providers { get; init; } = [];
}

public sealed class ListEmailProvidersRequestValidator : AbstractValidator<ListEmailProvidersRequest>
{
    public ListEmailProvidersRequestValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User Id is required.");
    }
}

public sealed class ListEmailProvidersRequestHandler(EmailProviderRepository emailProviderRepo)
    : IRequestHandler<ListEmailProvidersRequest, ListEmailProvidersResponse>
{
    public async ValueTask<Result<ListEmailProvidersResponse>> HandleAsync(
        ListEmailProvidersRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = new Result<ListEmailProvidersResponse>();

        #region # Execute

        var providers = await emailProviderRepo.ListAsync(request.UserId, cancellationToken);

        #endregion

        #region # Handle Result

        result.Success(new ListEmailProvidersResponse { Providers = providers });

        #endregion

        return result;
    }
}
