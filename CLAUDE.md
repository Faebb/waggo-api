# waggo-api

Backend of Waggo. .NET 10 · ASP.NET Core Minimal APIs · Clean Architecture · PostgreSQL + PostGIS · Docker · TDD. See `README.md` for setup.

This file is enough to work inside this repo. For the full workflow (specs, TDD agents, checklists), open Claude from the sibling repo `../waggo-workspace` (see its README).

## Commands

```bash
dotnet build                                                  # warnings are errors
dotnet test --filter "FullyQualifiedName!~IntegrationTests"   # fast TDD loop, no Docker
dotnet test                                                   # all, integration tests need Docker running
docker compose up -d --build                                  # API :8080 + PostGIS :5432
```

Run the build and the full test suite before declaring a task done.

## Rules

- **Code conventions (ADR-008, enforced by the build)**: never `var` (explicit types, target-typed `new()`); `_camelCase` instance fields, `s_camelCase` private static readonly, `PascalCase` constants; file-scoped namespace = folder; one type per file; `sealed` by default; braces always; `CancellationToken` last. Code/logs in English, **public messages in Spanish**. Run `dotnet format` before committing. Guide: vault `03 Desarrollo/Convenciones de código.md`.
- **TDD is mandatory**: failing test first, then code, then refactor. Outside-in: integration test of the endpoint, then Domain, then Application with NSubstitute doubles.
- Dependency rule `Api → Infrastructure → Application → Domain`, enforced by `Tests/Waggo.ArchitectureTests`. Domain references no project or framework.
- **Simplicity first**: code must be understandable at a glance. Fewer classes, nothing implicit.
- **Folder structure (ADR-012)**: layer projects at the repo root, tests in `Tests/` mirroring them. Folders by type (Clean Architecture template): Domain `Common/Entities/ValueObjects/Enums/Errors/Exceptions/Services`; Application `Common/{Interfaces,Extensions,Models,Constants}` + `<Module>/{Commands,Queries}/<UseCase>`; Infrastructure `Persistence/{Context,Configurations,Converters,Migrations}`, `Services`, `Options`; Api `Endpoints/<Module>/{Requests,Validators}` + `Infrastructure/{Middleware,Filters,Authentication,Authorization,Responses,Errors,Settings,Extensions,Helpers}`. Add a `<Module>/` subfolder inside type folders that grow. One type per file. Guide: vault `03 Desarrollo/Estructura de carpetas del backend.md`.
- **Responses (ADR-007)**: every operation returns `WaggoResponse<T>` (`Data`, `Errors`/`Warnings`/`Infos` lists, `IsValid`, `ErrorType`). Never throw for business rules — `response.AddError(...)`. After calling another operation: `response.ConcatStacks(other); if (!response.IsValid) { return response; }`. Set the result with `response.Data = ...`.
- **API platform**: `Program.cs` only composes `AddApplication/AddInfrastructure/AddWaggoApi` + `UseWaggoPipeline` + `MapWaggoEndpoints` (`Waggo.Api/Infrastructure/Extensions/`). Keep the middleware order documented there (exceptions → Serilog → status pages → CORS → authn → authz → rate limiter). Every endpoint declares `.RequireAuthorization(WaggoPolicies.X)` (fallback policy = authenticated). Health: `/health/live`, `/health/ready`.
- **Errors (ADR-010, hybrid)**: accumulable/expected errors → `WaggoResponse.AddError`; when the operation cannot continue → throw a custom exception from `Waggo.Domain/Exceptions` (`NotFoundException`, `ConflictException`, `ForbiddenException`, `UnauthorizedException`, `BusinessRuleException`, `ExternalServiceException`) with a public `Error` and optional technical detail. `ExceptionHandlingMiddleware` maps it; never expose exception messages to the client.
- **Auth (ADR-011)**: OAuth 2.0 JWT bearer (provider-agnostic `Authentication:Jwt`), roles `owner`/`walker`/`admin` (`WaggoRoles`). Development uses a fake user (`Authentication:UseDevelopmentUser`), roles overridable with header `X-Dev-Roles` and user with `X-Dev-User-Id`. Use cases get the caller through `ICurrentUser` (Application), never from `HttpContext`.
- **Persistence**: EF Core, `snake_case` (`EFCore.NamingConventions`), one schema per module, entities persisted directly. Migrations with the local tool: `dotnet tool restore` then `dotnet ef migrations add <Name> -p Waggo.Infrastructure -s Waggo.Api -o Persistence/Migrations`; they are applied on startup only in Development/Testing. Sensitive columns use `EncryptedStringConverter` (AES-256-GCM, key `Encryption:Key`, RNF-003). Positions: the domain has `GeoPoint`; `GeoPointConverter` maps it to a PostGIS `geography(Point, 4326)` (NetTopologySuite only in Infrastructure).
- **Validation (ADR-009, FluentValidation, two layers)**: (1) request DTO validator `internal sealed <X>RequestValidator` in `Waggo.Api/Endpoints/<Module>/Validators/`, run with `.WithRequestValidation<TRequest>()` (codes `Request.Required`/`Request.InvalidValue`); (2) business validator `internal sealed <X>QueryValidator`/`<X>CommandValidator` next to the handler, rules use `.WithError(<Module>Errors.X)`. First step of every handler: `response.ConcatStacks(await validator.ValidateToResponseAsync(query, ct)); if (!response.IsValid) { return response; }`. Domain keeps its own invariants.
- **Logging**: Serilog behind `ILogger<T>`. Methods do not log results; the caller writes the stacks once with `response.WriteLogs(logger, "Operation")` (normally the endpoint). `Internal` messages go only to the log.
- **Endpoints**: `response.WriteLogs(logger, "Op"); return response.ToApiResult();` (or `ToPagedApiResult()`) (→ `WaggoApiResponse<T>`, `pagination` null when not paged). Never `Results.Ok` / `Results.Problem`. Error codes `Module.Description`; errors catalog in Domain (`PricingErrors`), warning/info codes in Application (`PricingMessages`). Guide: vault `03 Desarrollo/Convención de respuestas y logging.md`.
- Own `ICommandHandler` / `IQueryHandler`; handlers are `sealed` and `internal`. No MediatR, AutoMapper or FluentAssertions (ADR-006) — use Shouldly.
- Package versions go in `Directory.Packages.props` only.
- Integration tests use real PostgreSQL via Testcontainers, never SQLite or InMemory.
- Test names: `Method_Scenario_ExpectedResult`. Commits: Conventional Commits, `test(module)` → `feat(module)` → `refactor(module)`.
- Never store card data (RNF-004); only payment gateway tokens.

## Project documentation

Spanish docs live in the Obsidian vault "Vault Waggo", expected at `../../../Vault Waggo` from this repo (disk layout in `waggo-workspace/README.md`; another path can be set with the `WAGGO_VAULT` environment variable). Index: `00 - MOC Waggo.md`. If the vault is not available, work from this file and the code, and say which note you could not read. Relevant notes: `02 Arquitectura/Backend - Clean Architecture.md`, `Base de datos - PostgreSQL.md`, ADRs in `02 Arquitectura/ADR/`, `03 Desarrollo/Flujo TDD.md`, and the Spanish ↔ code glossary `01 Proyecto/Glosario.md`.

When a change closes or alters an RF, update `04 Planeación/Estado de implementación.md`. When it changes a decision, packages or structure, update the matching note or ADR.
