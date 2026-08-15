namespace Application.Features.Workspace.Contacts.Commands;

using Application.Features.Workspace.Contacts;
using FluentValidation;

public sealed record DeleteContactRequest(Guid UserId, Guid ContactId) : ICommand<DeleteContactResponse>;

public sealed class DeleteContactResponse
{
    public bool Deleted { get; init; }
}

public sealed class DeleteContactRequestValidator : AbstractValidator<DeleteContactRequest>
{
    public DeleteContactRequestValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("User Id is required.");
        RuleFor(x => x.ContactId).NotEmpty().WithMessage("Contact Id is required.");
    }
}

public sealed class DeleteContactRequestHandler(ContactRepository contactRepo)
    : IRequestHandler<DeleteContactRequest, DeleteContactResponse>
{
    public async ValueTask<Result<DeleteContactResponse>> HandleAsync(
        DeleteContactRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = new Result<DeleteContactResponse>();

        #region # Execute

        var deleted = await contactRepo.DeleteAsync(
            request.UserId,
            request.ContactId,
            request.UserId,
            cancellationToken);

        #endregion

        #region # Handle Result

        if (!deleted)
        {
            result.Failure(ErrorCode.NotFound, "Contact not found.");
        }
        else
        {
            result.Success(new DeleteContactResponse { Deleted = true });
        }

        #endregion

        return result;
    }
}
