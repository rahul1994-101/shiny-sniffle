namespace Application.Features.workspace.Contacts.Commands;

using Application.Features.workspace.Contacts;
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
        var deleted = await contactRepo.SoftDeleteAsync(
            request.UserId,
            request.ContactId,
            request.UserId,
            cancellationToken);

        if (!deleted)
        {
            result.Failure(ErrorCode.NotFound, "Contact not found.");
        }
        else
        {
            result.Success(new DeleteContactResponse { Deleted = true });
        }

        return result;
    }
}
