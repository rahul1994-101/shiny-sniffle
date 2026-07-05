# MediatR (internal library)

Custom CQRS dispatcher for ShinySniffle. **Not** the Jimmy Bogard NuGet package. Folder stays `lib/MediatR` until a future NuGet publish.

**Status:** v1.0.0 shipped — WebApp on `IMediator`; **parked** while app features take priority.

## Flows

| Flow | API | Handlers | Returns |
|------|-----|----------|---------|
| Commands / queries | `IMediator.SendAsync` | `IRequestHandler<TRequest, TResult>` (1:1) | `Result` / `Result<T>` |
| Notifications | `IMediator.PublishAsync` | `INotificationHandler<TNotification>` (1:N) | nothing |

Pipeline: `ValidationBehavior` → `ExceptionBehavior` → handler. FluentValidation bundled in `AddMediatR`.

## Registration

```csharp
services.AddMediatR(Assembly.GetExecutingAssembly());
```

Registers validators, handlers, behaviors, and `IMediator` from one assembly.

## Handler pattern (consumer apps)

```csharp
public sealed record SignInRequest(...) : ICommand<SignInResponse>;

public sealed class SignInRequestHandler(UserRepository userRepo, SharedRepository sharedRepo)
    : IRequestHandler<SignInRequest, SignInResponse>
{
    public async ValueTask<Result<SignInResponse>> HandleAsync(SignInRequest request, CancellationToken ct = default)
    {
        var result = new Result<SignInResponse>();
        // result.Success(...) or result.Failure(...)
        return result;
    }
}
```

Use-case conventions: `.cursor/rules/feature-slice-conventions.mdc`.

## Namespaces

```text
MediatR.Abstractions       IRequest, ICommand, IQuery, INotification, IMediator
MediatR.Results            Result, Result<T>, Error, ErrorCode
MediatR.Behaviors          ValidationBehavior, ExceptionBehavior
MediatR.DependencyInjection   AddMediatR
```

## Version roadmap

| Version | Focus |
|---------|--------|
| **v1.0** ✅ | Send pipeline, `Result`, `AddMediatR`, FluentValidation, `FrozenDictionary` dispatch |
| **v1.1** | Notifications in WebApp (prove Publish path) |
| **v1.2** | Exception hardening — safe client messages |
| **v1.3** | `AddMediatR` options (extra behaviors, assemblies) |
| **v2.0** | Source generator — compile-time registration |
| **v2.1** | Unit/integration tests |
| **v3.0** | NuGet publish (`ShrewdSquad.Mediator`), namespace rename |

**Default path:** v1.0 → v1.1 → v1.2 → v2.1 → v3.0. v1.3–v1.4 and v2.0 when needed.

## Stays outside the lib

Repositories, EF, entities, feature DTOs/validators/handlers, AI orchestration, HTTP/Blazor — hosts consume `IMediator` only.

## Principles

| Topic | Decision |
|-------|----------|
| FluentValidation | Bundled in lib; validators live in feature slices |
| Repositories | WebApp only |
| AI / orchestration | May bypass mediator |
| Lifetime | Scoped mediator + handlers |
| Breaking changes | v2+ only, with migration notes |
