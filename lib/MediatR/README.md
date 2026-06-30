# MediatR (internal library)

Custom CQRS dispatcher for ShinySniffle. Inspired by [martinothamar/Mediator](https://github.com/martinothamar/Mediator) and MediatR-style pipelines.

May be published as a standalone NuGet package later. Lives in `lib/MediatR`, referenced by `WebApp` only.

---

## Three flows

| Flow | API | Handlers | Returns |
|------|-----|----------|---------|
| **Commands** | `IMediator.SendAsync` | `IRequestHandler<TRequest, TResult>` (1:1) | `Result` or `Result<T>` |
| **Queries** | `IMediator.SendAsync` | `IRequestHandler<TRequest, TResult>` (1:1) | `Result<T>` |
| **Notifications** | `IMediator.PublishAsync` | `INotificationHandler<TNotification>` (1:N) | nothing |

Commands and queries share the same Send pipeline and behaviors. Notifications are a separate dispatch path with no `Result` envelope.

---

## Namespaces

```text
MediatR.Abstractions       IRequest, ICommand, IQuery, INotification, IMediator, handlers
MediatR.Results            Result, Result<T>, Error, ErrorCode
MediatR.Pipeline           RequestPipeline
MediatR.Behaviors          ValidationBehavior, ExceptionBehavior
MediatR.Dispatch           Mediator (internal), dispatch tables
MediatR.DependencyInjection   AddMediatR(assembly)
```

---

## Fixed return types (`MediatR.Results`)

| Type | When |
|------|------|
| `Result` | `ICommand` (no response payload) |
| `Result<T>` | `ICommand<T>` or `IQuery<T>` |

Supporting types: `Error`, `ErrorCode`.

**Future:** static helpers on `Result` (e.g. `Result.Success(payload)`, `Result.Failure(code, message)`).

---

## Request markers

```csharp
public sealed record SignInRequest(...) : ICommand<SignInResponse>;
public sealed record DeleteRequest(...) : ICommand;
public sealed record GetSettingsRequest(...) : IQuery<SettingsResponse>;
public sealed record UserSignedIn(Guid UserId) : INotification;
```

Handlers:

```csharp
IRequestHandler<SignInRequest, Result<SignInResponse>>
IRequestHandler<DeleteRequest, Result>
INotificationHandler<UserSignedIn>   // PublishAsync — no Result
```

---

## Registration

```csharp
services.AddMediatR(Assembly.GetExecutingAssembly());
```

Registers request handlers, notification handlers, validators, pipeline behaviors, and `IMediator`.

---

## Migration phases

### Phase 0 — Scaffold

- [x] Project + solution + WebApp reference
- [x] README
- [x] `MediatR.Results` moved out of WebApp

### Phase 1 — Core lib

- [x] Abstractions (`ICommand`, `IQuery`, `INotification`, `IMediator`, handlers)
- [x] Pipeline, behaviors, dispatch
- [x] `SendAsync` + `PublishAsync`
- [x] `AddMediatR` replaces `AddFeatureLayer`
- [x] `ValueTask` on mediator, handlers, behaviors
- [x] Payload-only `ICommand<T>` / `IQuery<T>`
- [x] WebApp handlers, Blazor, `AuthEndpoints` on `IMediator`
- [x] Deleted `WebApp/Features/Shared/Cqrs/`
- [x] Build passes

### Phase 2 — Performance

- [ ] `FrozenDictionary` dispatch table
- [ ] Typed `SendAsync<TRequest>` without boxing / `Activator`

### Phase 3 — Source generator (optional)

- [ ] Compile-time handler registry + DI

---

## Notes

- **WebApp** keeps slices, validators, repos; `AddFeatureRepositories()` stays in WebApp.
- **AI / orchestration** may keep direct repo access.
- **Scoped** lifetime for mediator and handlers.
