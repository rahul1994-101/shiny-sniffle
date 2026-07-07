# ShinySniffle

Blazor Server workspace app: vertical-slice features, custom CQRS (`lib/MediatR`), Azure AI Foundry agents (Assistant + Email).

## Solution

```text
lib/MediatR/              CQRS dispatcher
src/Core/                 Entities only
src/Infrastructure/       EF, Mailbox, Foundry (ports + DTOs.cs per folder)
src/WebApp/               UI, Features, AI, Startup
databases/Database/       SQL Server project
```

## Documentation

| Doc | Purpose |
|-----|---------|
| [`.cursor/rules/`](.cursor/rules/) | Coding conventions for agents and humans |
| [`lib/MediatR/README.md`](lib/MediatR/README.md) | CQRS lib API and version roadmap |
| [`docs/README.md`](docs/README.md) | Feature roadmaps (AI memory, email read) |

## Quick start

1. Configure connection string and Foundry (`Foundry:Enabled`, `Endpoint`, `ApiKey` in user secrets).
2. Publish or run migrations from `databases/Database/`.
3. Run `src/WebApp`.
