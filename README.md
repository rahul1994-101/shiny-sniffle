# ShinySniffle

Blazor Server workspace app: vertical-slice features, custom CQRS (`lib/MediatR`), Azure AI Foundry agents (Assistant + Email).

## Solution

```text
lib/MediatR/              CQRS dispatcher
src/Infrastructure/       Persistence, Mailbox, Foundry + AddInfrastructure()
src/Application/          Features, AI, app services + AddApplication()
src/WebApp/               Blazor UI, Endpoints, Startup
```

## Documentation

| Doc | Purpose |
|-----|---------|
| [`.cursor/rules/`](.cursor/rules/) | Coding conventions — one rule file per project |
| [`lib/MediatR/README.md`](lib/MediatR/README.md) | CQRS lib API and version roadmap |
| [`docs/product.md`](docs/product.md) | Business / product spec (single doc) |
| [`docs/README.md`](docs/README.md) | Technical roadmaps index |

## Quick start

1. Configure connection string and Foundry (`Foundry:Enabled`, `Endpoint`, `ApiKey` in user secrets).
2. Apply SQL from `src/Infrastructure/Persistence/` as needed (`dbo/Tables/`, `chat/Tables/`, `workspace/Tables/`). SQL schemas (`chat`, `workspace`, …) are provisioned outside these scripts. Uncomment **SEED DATA** in `dbo/Tables/EmailProvider.sql` when bootstrapping a new database.
3. Run `src/WebApp`.
