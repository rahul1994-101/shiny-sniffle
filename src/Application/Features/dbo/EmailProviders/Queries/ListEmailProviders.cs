namespace Application.Features.dbo.EmailProviders.Queries;

using Application.Features.dbo.EmailProviders;

public sealed record ListEmailProvidersRequest : IQuery<ListEmailProvidersResponse>;

public sealed class ListEmailProvidersResponse
{
    public IReadOnlyList<EmailProviderDto> Providers { get; init; } = [];
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

        var providers = await emailProviderRepo.ListAsync(cancellationToken);

        #endregion

        #region # Handle Result

        result.Success(new ListEmailProvidersResponse { Providers = providers });

        #endregion

        return result;
    }
}
