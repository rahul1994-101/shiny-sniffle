namespace Application.Features.workspace.Buckets.Commands;

using Application.Features.workspace.Buckets;
using FluentValidation;

public sealed record SaveBucketRequest(Guid UserId, SaveBucketDto Bucket) : ICommand<SaveBucketResponse>;

public sealed class SaveBucketResponse : BucketDto;

public sealed class SaveBucketRequestValidator : AbstractValidator<SaveBucketRequest>
{
    public SaveBucketRequestValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Bucket).NotNull();
    }
}

public sealed class SaveBucketRequestHandler(BucketRepository bucketRepo)
    : IRequestHandler<SaveBucketRequest, SaveBucketResponse>
{
    public async ValueTask<Result<SaveBucketResponse>> HandleAsync(
        SaveBucketRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = new Result<SaveBucketResponse>();
        var validation = BucketMapping.ValidateSave(request.Bucket);
        if (validation is not null)
        {
            result.Failure(ErrorCode.BadRequest, validation);
            return result;
        }

        var (saved, error, notFound) = await bucketRepo.SaveAsync(
            request.UserId,
            request.Bucket,
            request.UserId,
            cancellationToken);

        if (notFound)
        {
            result.Failure(ErrorCode.NotFound, "Bucket not found.");
        }
        else if (error is not null)
        {
            result.Failure(ErrorCode.BadRequest, error);
        }
        else if (saved is null)
        {
            result.Failure(ErrorCode.InternalServerError, "Failed to save bucket.");
        }
        else
        {
            result.Success(new SaveBucketResponse
            {
                Id = saved.Id,
                Name = saved.Name,
                Alias = saved.Alias,
                Color = saved.Color,
                Context = saved.Context,
                SortOrder = saved.SortOrder
            });
        }

        return result;
    }
}
