# JustGo Platform — Copilot Instructions

Use this file as the quick operating guide. For full details, follow links instead of duplicating rules.

## Read First

- Architecture and boundaries: [docs/DDD-Modular-Monolith-Architecture.md](../docs/DDD-Modular-Monolith-Architecture.md)
- Agent workflow and patterns: [AGENTS.md](../AGENTS.md)
- Environment/setup details: [README.md](../README.md)
- Layer-specific conventions:
  - [application-layer.instructions.md](instructions/application-layer.instructions.md)
	- [backend-queries.instructions.md](instructions/backend-queries.instructions.md)
  - [controllers.instructions.md](instructions/controllers.instructions.md)
  - [unit-tests.instructions.md](instructions/unit-tests.instructions.md)
- Reusable prompts:
	- [backend-queries-audit.prompt.md](prompts/backend-queries-audit.prompt.md)

## Critical Workflow

1. Keep changes minimal and consistent with existing module patterns.
2. Preserve modular boundaries:
	- Domain cannot reference sibling modules or `JustGoAPI.Shared`.
	- Application cannot reference sibling Application projects.
	- Cross-module communication goes through MediatR requests.
3. Before finishing, run:
	- `dotnet build`
	- `dotnet test`
	- `dotnet test tests/JustGo.ArchitectureTests`

## Project-Specific Rules

- CQRS + MediatR per use case (`IRequest<T>` + handler).
- Use `LazyService<IReadRepository<T>>` / `LazyService<IWriteRepository<T>>` in handlers.
- Use parameterized SQL through repositories. For inline SQL, pass `commandType: "text"`.
- Wrap API responses with `ApiResponse<TData, TError>` and honor `CancellationToken`.
- Use FluentValidation validators in Application layer.
- Use Mapster for mapping (not AutoMapper).

## Security And Quality Gates

- Enforce authorization and ownership checks on protected endpoints.
- Validate all input and avoid raw string-concatenated SQL.
- Avoid logging secrets or sensitive tenant/user data.
- Add or update unit tests for behavior changes.
- Add integration/API tests for main flows when endpoint behavior changes.

## Completion Checklist

Include in final update:

- Files changed
- Tests added/updated
- OWASP A01-A10 risk coverage summary
- Commands used to verify locally

