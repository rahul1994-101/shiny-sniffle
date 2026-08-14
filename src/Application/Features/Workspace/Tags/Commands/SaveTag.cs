namespace Application.Features.Workspace.Tags.Commands;



using Application.Features.Workspace.Tags;

using FluentValidation;



public sealed record SaveTagRequest(Guid UserId, SaveTagDto Tag) : ICommand<SaveTagResponse>;



public sealed class SaveTagResponse : TagDto;



public sealed class SaveTagRequestValidator : AbstractValidator<SaveTagRequest>

{

    public SaveTagRequestValidator()

    {

        RuleFor(x => x.UserId).NotEmpty();

        RuleFor(x => x.Tag).NotNull();

    }

}



public sealed class SaveTagRequestHandler(TagRepository tagRepo)

    : IRequestHandler<SaveTagRequest, SaveTagResponse>

{

    public async ValueTask<Result<SaveTagResponse>> HandleAsync(

        SaveTagRequest request,

        CancellationToken cancellationToken = default)

    {

        var result = new Result<SaveTagResponse>();



        #region # Execute



        var validation = TagMapping.ValidateSave(request.Tag);

        if (validation is not null)

        {

            result.Failure(ErrorCode.BadRequest, validation);

            return result;

        }



        var (saved, error, notFound) = await tagRepo.SaveAsync(

            request.UserId,

            request.Tag,

            request.UserId,

            cancellationToken);



        #endregion



        #region # Handle Result



        if (notFound)

        {

            result.Failure(ErrorCode.NotFound, "Tag not found.");

        }

        else if (error is not null)

        {

            result.Failure(ErrorCode.BadRequest, error);

        }

        else if (saved is null)

        {

            result.Failure(ErrorCode.InternalServerError, "Failed to save tag.");

        }

        else

        {

            result.Success(saved.AsResponse<SaveTagResponse>());

        }



        #endregion



        return result;

    }

}

