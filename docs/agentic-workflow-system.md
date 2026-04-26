# Agentic Workflow System for JustGo Platform

## Purpose

This document captures recommendations for designing an agentic workflow system tailored to the JustGo platform. The goal is to improve engineering productivity while preserving the existing DDD modular monolith boundaries, CQRS conventions, and multi-tenancy guarantees.

## System Context

The current platform is a DDD modular monolith with:

- Multiple business modules under `src/Modules/`
- Layered module structure: API, Application, Domain, Infrastructure
- CQRS with MediatR
- Raw SQL repository pattern via `IReadRepository<T>` and `IWriteRepository<T>`
- Multi-tenancy via `TenantContextManager` and `DatabaseSwitcher`
- Architecture tests enforcing module boundaries

Any agentic workflow should reinforce these constraints rather than bypass them.

## Recommended Custom Agents

### 1. Module Scaffold Agent

Creates new modules, bounded contexts, and feature skeletons that match the repository conventions.

Responsibilities:

- Generate module folder structure for Domain, Application, Infrastructure, and API
- Scaffold `Add{Module}Services()` registration patterns
- Create feature directories under `Features/{Area}/{V1|V2}/Commands|Queries/{FeatureName}/`
- Generate initial handlers, validators, DTOs, and controller shells
- Prepare matching unit test skeletons
- Run architecture tests after generation

Best use cases:

- Adding a new module
- Adding a new bounded context inside an existing module
- Starting a new command or query feature with minimal manual setup

### 2. CQRS Feature Agent

Automates end-to-end feature creation for common command/query workflows.

Responsibilities:

- Generate `IRequest<T>` request objects
- Generate `IRequestHandler<TRequest, TResponse>` handlers
- Add FluentValidation validators
- Create DTOs and Mapster mappings where needed
- Generate controller endpoints with `ApiResponse<TData, TError>` wrapping
- Generate mock-driven unit tests using `LazyServiceMockHelper`
- Suggest or scaffold repository SQL access patterns

Best use cases:

- New query endpoints
- CRUD-like internal features
- Repetitive application-layer work across modules

### 3. Multi-Tenancy Coordinator Agent

Protects and automates tenant-aware workflows.

Responsibilities:

- Validate tenant context usage before data access
- Detect missing `TenantContextManager` or `DatabaseSwitcher` calls in relevant flows
- Suggest tenant-safe query patterns
- Ensure credentials and tenant connection data are handled consistently
- Insert audit and logging hooks for tenant-sensitive operations

Best use cases:

- Features that switch between central and tenant databases
- Background jobs with tenant-specific execution
- Cross-tenant administrative workflows

### 4. Architecture Enforcer Agent

Continuously protects module boundaries and design rules.

Responsibilities:

- Detect forbidden cross-module references
- Suggest MediatR-based alternatives instead of direct module coupling
- Validate that Domain remains dependency-light
- Validate that Application layers do not directly depend on sibling Application projects
- Check whether new code matches existing modular-monolith rules

Best use cases:

- Pull request review
- Pre-commit validation
- Refactoring work that touches module dependencies

### 5. API Testing and Contract Agent

Improves delivery quality for controllers and external contracts.

Responsibilities:

- Generate API test skeletons from controllers
- Check endpoint consistency for authorization, route constraints, and response wrappers
- Generate or update contract-focused documentation
- Suggest coverage for error responses and validation failures
- Build Postman or HTTP test assets if needed

Best use cases:

- Adding new endpoints
- Regression protection for existing APIs
- Improving consistency across controllers

## Recommended Custom Skills

### 1. DDD-to-Code Translator

Transforms domain language into repository-compliant code structures.

Examples:

- Turn a business rule into aggregate methods, commands, validators, and events
- Convert use-case descriptions into CQRS features
- Propose aggregate boundaries and domain event names

### 2. SQL-to-Repository Converter

Converts SQL intent into safe repository usage aligned with the codebase.

Examples:

- Build parameterized SQL snippets for handlers
- Select between `GetAsync`, `GetListAsync`, `GetSingleAsync`, and write operations
- Enforce `commandType: "text"` for raw SQL

### 3. Module Boundary Analyzer

Reviews whether changes preserve the modular monolith.

Examples:

- Detect over-coupled modules
- Flag dependency drift
- Suggest when logic should move to Contracts, Shared Kernel, or MediatR requests

### 4. Multi-Tenancy Safety Validator

Reviews tenant isolation and runtime safety.

Examples:

- Verify tenant filters exist where needed
- Confirm correct database switching path
- Ensure tenant-sensitive operations are audited

### 5. CQRS Pattern Enforcer

Maintains consistency across the application layer.

Examples:

- Ensure handlers use `LazyService<IReadRepository<T>>` or `LazyService<IWriteRepository<T>>`
- Ensure validators exist when input exists
- Ensure controllers stay thin and delegate via MediatR
- Ensure feature placement follows repository instructions

## Recommended Hooks

### 1. Pre-Commit Hook: Architecture and Test Validation

Run focused checks before code is committed.

Suggested checks:

- `dotnet test tests/JustGo.ArchitectureTests`
- Module-specific unit tests for touched modules
- Fast validation for changed API or Application code

Expected benefit:

- Catch module boundary violations early
- Prevent invalid dependency additions from landing in git history

### 2. MediatR Pipeline Hook: Agent Instrumentation

Add a pipeline behavior that gives agent workflows a reliable place to observe request execution.

Suggested responsibilities:

- Log request type and module context
- Record tenant context metadata
- Enforce cross-cutting checks before handler execution
- Emit audit events after successful operations
- Attach timing and failure diagnostics for later analysis

Example design:

```csharp
public class AgentInstrumentationBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Record request metadata, tenant context, and timing here.
        var response = await next();
        // Emit audit/telemetry hooks here.
        return response;
    }
}
```

### 3. Pull Request Hook: Quality Gate Agent

Trigger automated review checks when a PR is opened or updated.

Suggested checks:

- Architecture test status
- Missing validators for new commands/queries
- Missing authorization attributes on new endpoints
- Missing response wrappers or route constraints
- Missing unit tests for new handlers

Expected benefit:

- Turns review into higher-signal feedback
- Reduces time spent on repeatable comments

### 4. Build Hook: Code Generation and Consistency Checks

Use build-time or CI-time automation to fill low-risk boilerplate gaps.

Suggested checks:

- Missing mapping registrations
- Missing module service registration
- Missing DTO or test skeletons for newly created features
- Inconsistent feature folder placement

Expected benefit:

- Reduce repetitive manual work
- Keep the codebase consistent as the number of modules grows

### 5. Git Workflow Hook: Feature Bootstrap

Trigger scaffolding when a new feature branch or task context is created.

Suggested behavior:

- Parse branch names such as `feature/booking/transfer-attendee`
- Pre-create feature folders and test shells
- Suggest related modules and conventions
- Prompt the developer with a starter checklist

Expected benefit:

- Faster startup for common work
- Better naming and feature organization consistency

## Suggested Priority Order

### Phase 1: Highest ROI

Start with the pieces that remove repetitive work and prevent architecture regressions.

- Module Scaffold Agent
- CQRS Feature Agent
- Pre-commit architecture hook
- Pull request quality gate

### Phase 2: Safety and Observability

Add the pieces that protect correctness and make workflows inspectable.

- Multi-Tenancy Coordinator Agent
- Multi-Tenancy Safety Validator
- MediatR agent instrumentation behavior

### Phase 3: Advanced Repository-Specific Automation

Add the pieces that lean into your platform’s specific patterns.

- SQL-to-Repository Converter
- CQRS Pattern Enforcer
- Feature branch bootstrap automation
- Build-time consistency checks

## Concrete Custom Agent Ideas for This Repository

These are especially aligned with the current structure of the JustGo platform.

### A. New Module Agent

Input:

- Module name
- Domain summary
- Initial bounded contexts
- Required API surface

Output:

- Full module skeleton under `src/Modules/`
- Service registration wiring
- Initial tests
- Architecture validation run

### B. New Feature Agent

Input:

- Module
- Area
- Command or query name
- Response DTO
- Business rules

Output:

- Request, handler, validator, DTO, controller action, tests
- SQL placeholders or stored procedure integration points
- Notes about tenant context if relevant

### C. Controller Review Agent

Input:

- One or more controller files

Output:

- Missing `[CustomAuthorize]` or route constraints
- Missing `ProducesResponseType` metadata
- Response wrapping inconsistencies
- Suggestions for thinner controllers

### D. Handler Review Agent

Input:

- One or more application feature folders

Output:

- Missing validators
- Direct repository injection instead of `LazyService<T>`
- Suspicious SQL command type usage
- Opportunities to split command/query responsibilities

### E. Tenant Safety Review Agent

Input:

- Any feature that touches tenant-aware data or DB switching

Output:

- Tenant isolation concerns
- Missing context propagation
- Auditing recommendations
- Safer execution flow suggestions

## Suggested Skill Library Layout

If you want to formalize these in repository instructions and reusable prompts, a practical structure would be:

```text
.github/
  instructions/
    application-layer.instructions.md
    controllers.instructions.md
    unit-tests.instructions.md
  skills/
    module-scaffold/
      SKILL.md
    cqrs-feature/
      SKILL.md
    architecture-enforcer/
      SKILL.md
    multi-tenancy-safety/
      SKILL.md
    sql-repository/
      SKILL.md
```

Each skill should define:

- When to invoke it
- Inputs expected from the user
- Constraints from the codebase
- Required validations after generation
- What not to do in this repository

## Suggested Hooks to Add First

If implementation time is limited, start with these three:

1. Pre-commit architecture hook
2. Pull request quality gate agent
3. CQRS feature generation agent

That combination gives the best balance of speed, consistency, and architectural safety.

## Expected Productivity Gains

If implemented well, the workflow should improve:

- Feature startup speed by removing repetitive scaffolding
- Review quality by automating rule-based checks
- Architectural consistency across modules
- Tenant-safety confidence in shared infrastructure flows
- Onboarding speed for new developers working inside the modular monolith

## Final Recommendation

Design the agentic system around the repository’s existing rules, not around generic AI automation. In this codebase, the biggest gains will come from:

- Enforcing module boundaries automatically
- Generating CQRS boilerplate consistently
- Validating tenant-aware flows before bugs reach production
- Standardizing controller and handler quality checks

A small number of focused agents and hooks will produce more value than one large general-purpose agent.
