using Application.Features.EmailProviders;
using FluentValidation;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.UserSettings.Commands;

public sealed record SaveEmailSettingsRequest(Guid UserId, EmailSettingsDto Email)
    : ICommand<SaveEmailSettingsResponse>;

public sealed class SaveEmailSettingsResponse : EmailSettingsDto
{
}

public sealed class SaveEmailSettingsRequestValidator : AbstractValidator<SaveEmailSettingsRequest>
{
    public SaveEmailSettingsRequestValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User Id is required.");

        RuleFor(x => x.Email)
            .NotNull()
            .WithMessage("Email settings are required.");
    }
}

public sealed class SaveEmailSettingsRequestHandler(
    IDbContextFactory<AppDbContext> dbContextFactory,
    UserSettingsRepository userSettingsRepo,
    EmailProviderRepository emailProviderRepo)
    : IRequestHandler<SaveEmailSettingsRequest, SaveEmailSettingsResponse>
{
    public async ValueTask<Result<SaveEmailSettingsResponse>> HandleAsync(SaveEmailSettingsRequest request, CancellationToken cancellationToken = default)
    {
        var result = new Result<SaveEmailSettingsResponse>();

        var (catalog, catalogError) = await EmailSettingsCatalog.LoadCatalogAsync(emailProviderRepo, cancellationToken);
        if (catalogError is not null)
        {
            result.Failure(ErrorCode.BadRequest, catalogError);
            return result;
        }

        var email = request.Email;
        var applyError = EmailSettingsCatalog.TryApplyCatalog(email, catalog);
        if (applyError is not null)
        {
            result.Failure(ErrorCode.BadRequest, applyError);
            return result;
        }

        var existingSettings = await userSettingsRepo.GetUserEmailSettingsAsync(request.UserId, cancellationToken);
        var validationError = EmailSettingsMapping.TryBuildEntity(email, existingSettings, EmailSettingsBuildMode.Save, out var newSettings);

        if (validationError is not null)
        {
            result.Failure(ErrorCode.BadRequest, validationError);
            return result;
        }

        #region # Execute

        var savedSettings = await SaveUserEmailSettingsAsync(request.UserId, newSettings, request.UserId, cancellationToken);

        #endregion

        #region # Handle Result

        result.Success(EmailSettingsMapping.FromEntity(savedSettings).AsResponse<SaveEmailSettingsResponse>());

        #endregion

        return result;
    }

    #region # Private Helpers

    private async Task<EmailSettings?> SaveUserEmailSettingsAsync(Guid userId, EmailSettings? emailSettings, Guid updatedBy, CancellationToken cancellationToken)
    {
        await using var ctx = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var existing = await ctx.UserSettings
            .Where(x =>
                x.UserId == userId &&
                x.IsActive &&
                !x.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        var now = DateTime.UtcNow;
        var emailSettingsJson = EmailSettingsJsonHelpers.ToJson(emailSettings);

        if (existing is null)
        {
            var entity = new UserSetting
            {
                UserId = userId,
                EmailSettingsJson = emailSettingsJson,
                CreatedBy = updatedBy,
                UpdatedBy = updatedBy,
                CreatedAt = now,
                UpdatedAt = now
            };

            await ctx.UserSettings.AddAsync(entity, cancellationToken);
            await ctx.SaveChangesAsync(cancellationToken);
            return EmailSettingsJsonHelpers.FromJson(entity.EmailSettingsJson);
        }

        existing.EmailSettingsJson = emailSettingsJson;
        existing.UpdatedBy = updatedBy;
        existing.UpdatedAt = now;
        await ctx.SaveChangesAsync(cancellationToken);
        return EmailSettingsJsonHelpers.FromJson(existing.EmailSettingsJson);
    }

    #endregion
}
