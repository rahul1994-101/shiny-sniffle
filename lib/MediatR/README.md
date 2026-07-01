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

Handlers and pipeline use **instance-only** `Success()` / `Failure()` on `new Result` / `new Result<T>`. Declare `var result = new Result<T>();` as the first statement in `HandleAsync` (supports multi-step flows and early returns).

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
IRequestHandler<SignInRequest, SignInResponse>
IRequestHandler<DeleteRequest>                    // ICommand, no payload
INotificationHandler<UserSignedIn>
```

---

## Registration

**WebApp** (composition root):

```csharp
services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
services.AddMediatR(Assembly.GetExecutingAssembly());
```

`AddMediatR` registers request/notification handlers, pipeline behaviors, and `IMediator`. FluentValidation registration stays in the host so validators are optional and assembly choice is explicit.

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
- [x] Validator registration split to WebApp (`AddValidatorsFromAssembly` + `AddMediatR`)
- [x] Duplicate-handler guard at startup
- [x] Non-nullable `TResponse` on commands/queries
- [x] Build passes

**Notifications:** `PublishAsync` and `INotificationHandler` are registered by `AddMediatR`, but WebApp has no notifications yet. First real usage is **Phase 4**.

### Phase 2 — Performance

- [ ] `FrozenDictionary` dispatch table
- [ ] Typed `SendAsync<TRequest>` without boxing / `Activator`

### Phase 3 — Source generator (optional)

- [ ] Compile-time handler registry + DI

### Phase 4 — Notifications (adoption)

- [ ] First `INotification` + `INotificationHandler` in WebApp
- [ ] Call `IMediator.PublishAsync` from a use case (e.g. side effects after sign-in or thread create)
- [ ] Confirm 1:N dispatch when multiple handlers exist for one notification

### Phase 5 — FluentValidation decoupling

Validator **registration** is already in WebApp (`AddValidatorsFromAssembly`). `ValidationBehavior` still lives in `lib/MediatR` and requires the FluentValidation package.

- [ ] Decide approach: keep as-is, extract to `MediatR.FluentValidation`, or move behavior to WebApp
- [ ] Implement chosen split so core `MediatR` does not hard-depend on FV (if extracting)
- [ ] Update `Startup` / package references accordingly

### Phase 6 — Tests (after migration complete)

- [ ] Lib unit tests — dispatch table, pipeline behaviors, duplicate-handler guard
- [ ] Optional handler integration / smoke tests in WebApp

---

## Notes

- **WebApp** keeps slices, validators, repos; `AddFeatureRepositories()` stays in WebApp.
- **AI / orchestration** may keep direct repo access.
- **Scoped** lifetime for mediator and handlers.
