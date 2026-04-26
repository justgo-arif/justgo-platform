# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## First-time setup

```powershell
# Windows
./scripts/setup-dev.ps1
```

```bash
# WSL / bash
sh scripts/setup-dev.sh
```

Activates git hooks and generates `.claudeignore`, `.copilotignore`, `.cursorignore` from `.aiignore`. Run once after cloning.

## Commands

```bash
# Build
dotnet restore
dotnet build

# Run API (http://localhost:5152, Swagger at /swagger)
dotnet run --project src/JustGoAPI.API

# Run all tests
dotnet test

# Run specific module tests
dotnet test src/Modules/AssetManagementModule/tests/JustGo.AssetManagement.UnitTests

# Run architecture boundary tests (always run before committing)
dotnet test tests/JustGo.ArchitectureTests

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# EF migrations
dotnet ef database update --project src/JustGoAPI.API
```

## Architecture

**DDD Modular Monolith** — 11 business modules in `src/Modules/`, each strictly layered:

```
{ModuleName}/
├── {ModuleName}.Domain/          # Entities, aggregates, value objects — no external deps
├── {ModuleName}.Application/     # CQRS handlers (MediatR), FluentValidation, Mapster profiles
├── {ModuleName}.Infrastructure/  # DbContext, repositories, external adapters
└── {ModuleName}.API/             # Controllers + service registration extension
```

**Architectural rules enforced by tests** (`tests/JustGo.ArchitectureTests`):
- Domain projects must not reference sibling modules or `JustGoAPI.Shared`
- Application projects must not reference sibling Application projects
- Module boundaries are hard — cross-module calls go through MediatR, not direct DI

**Shared infrastructure:**
- `src/JustGo.Authentication/` — JWT auth, ABAC authorization, generic repositories, pagination, caching
- `src/JustGoAPI.Shared/` — shared DTOs, Mapster configuration, utilities
- `src/JustGoAPI.API/` — composition root: `Program.cs` wires everything

## Key Patterns

**CQRS via MediatR:** Every use case is a `IRequest<T>` + `IRequestHandler`. Commands mutate state; queries read. Handlers live in `{Module}.Application/Features/`.

**Repository pattern:** Generic `IReadRepository<T>` / `IWriteRepository<T>` in `JustGo.Authentication/Persistence/Repositories/GenericRepositories/`. Uses **raw parameterized SQL**, not EF LINQ — pass SQL strings directly to `GetListAsync()`.

**Module DI registration:** Each module exposes `services.Add{Module}Services()` in `{Module}.API/{Module}ServiceRegistration.cs`. All called from `Program.cs`.

**Multi-tenancy:** `TenantContextManager` + `IDatabaseProvider` switches connection strings at runtime. Two physical databases: `Development_286` (application data) and `restapi_common_db_v1` (tenant registry, encrypted credentials).

**ABAC authorization:** Custom `IAbacPolicyService` evaluates attribute-based policies. Use `[CustomAuthorize]` attribute on controllers. Located in `JustGo.Authentication/Infrastructure/AbacAuthorization/`.

**Mapping:** Mapster (not AutoMapper). Global config in `src/JustGoAPI.Shared/CustomAutoMapper/CustomMapsterProfile.cs`; per-module profiles in `{Module}.Application/MappingProfiles/`.

**Validation:** FluentValidation, auto-registered per module via assembly scanning. Runs as a MediatR pipeline behavior (`ValidationBehavior<TRequest, TResponse>`).

**Logging:** Serilog. Custom sinks: `EventSink`, `ExceptionSink`, `AuditSink`. Log file configured at `D:\Logs\log.txt` in dev.

**API versioning:** Query string (`api-version`), URL segment (`/v1`), or `X-Api-Version` header. Default version 1.0.

## Testing

- **Unit tests:** TUnit + Moq. Test handlers with mocked repositories.
- **Architecture tests:** XUnit + reflection on `.csproj` XML. Validates dependency rules.
- Test file pattern: `{Feature}.Tests.cs` inside `{Module}.UnitTests/`.

## Database Setup

Requires SQL Server 2022 (or Docker via `docker-compose up`). Restore two `.bacpac` backups (password: `justgobd1234`), then update connection strings in `src/JustGoAPI.API/appsettings.Development.json`. See `README.md` for full restore script and tenant data update instructions.

## Planned Refactoring (not yet implemented)

Per `docs/DDD-Modular-Modular-Architecture.md`:
- Split `AuthModule` into three modules: Identity, Tenancy, Authorization
- Add `{Module}.Contracts/` projects for inter-module events/interfaces
- Remove `MobileAppsModule` (delivery channel, not a domain)
- Rename `JustGoAPI.Shared` → `SharedKernel`

Do not implement these preemptively — wait for explicit instruction.
