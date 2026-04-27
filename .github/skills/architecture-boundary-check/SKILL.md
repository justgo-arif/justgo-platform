---
name: architecture-boundary-check
description: 'Run architecture boundary tests and diagnose DDD module violations. Use when: validating module dependencies before commit, fixing cross-module reference errors, verifying Domain/Application layer isolation, checking MediatR compliance. Commands: /architecture-boundary-check or automatic on architecture queries.'
argument-hint: 'Optional: specific module name (e.g., AuthModule, ClubModule) to check only that module'
user-invocable: true
---

# Architecture Boundary Check

## When to Use

**Always run before committing** — this skill validates the modular monolith's DDD boundaries.

Use this skill when:
- Preparing a pull request or commit
- You see dependency violations in your IDE
- Adding cross-module communication
- Refactoring module structure
- Onboarding to the project (learn the boundary rules)

## Quick Start

```bash
dotnet test tests/JustGo.ArchitectureTests
```

**What to expect:**
- ✅ **PASS**: All modules respect boundaries → safe to commit
- ❌ **FAIL**: Violations listed → scroll down for fix patterns

## The Three Boundary Rules

The JustGo Platform enforces **three immutable DDD rules**. Every rule failure gets a fix pattern below.

### Rule 1: Domain Must Not Reference Sibling Modules

**Violation**: A Domain project depends on another module's project (e.g., `BookingModule.Domain` → `ClubModule.Domain`)

**Why it fails**: Domain is the core of DDD — it represents pure business rules with **zero external dependencies**.

**Fix pattern**:
- ❌ **WRONG**: `BookingModule.Domain` references `ClubModule.Domain.Entities`
- ✅ **RIGHT**: Move the shared concept to `JustGo.Authentication/Contracts/` (Shared Kernel) or represent it as a value object/ID in BookingModule

**Code example**:
```csharp
// ❌ BookingModule.Domain/Entities/Booking.cs
using ClubModule.Domain.Entities;  // VIOLATION
public class Booking
{
    public Club Club { get; set; }  // Direct dependency on Club entity
}

// ✅ Refactored
public class Booking
{
    public Guid ClubId { get; set; }  // Store only the ID, not the entity
    
    // If you need club data, fetch it at Application layer via MediatR
}
```

### Rule 2: Domain Must Not Reference JustGoAPI.Shared

**Violation**: A Domain project includes `using JustGoAPI.Shared.*`

**Why it fails**: Domain is self-contained. Shared utilities (DTOs, mapping, configs) belong at Application or API layers.

**Fix pattern**:
- ❌ **WRONG**: `AuthModule.Domain` uses `JustGoAPI.Shared.DTOs.TokenDto`
- ✅ **RIGHT**: Domain uses only primitives and pure domain types; DTOs live in Application/API layers

**Code example**:
```csharp
// ❌ AuthModule.Domain/Entities/User.cs
using JustGoAPI.Shared;
public class User
{
    public UserDto ToDto() { ... }  // VIOLATION: DTO logic in Domain
}

// ✅ Refactored (Domain stays pure)
public class User
{
    public string Email { get; set; }
    public string FullName { get; set; }
    // No DTO references
}

// ✅ DTO mapping lives in Application layer
// AuthModule.Application/MappingProfiles/UserMappingProfile.cs
public class UserMappingProfile : CustomMapsterProfile
{
    public override void Register(TypeAdapterConfig config)
    {
        config.NewConfig<User, UserDto>()
            .Map(dest => dest.FullName, src => src.FullName);
    }
}
```

### Rule 3: Application Must Not Reference Sibling Application Projects

**Violation**: One module's Application references another module's Application (e.g., `BookingModule.Application` → `ClubModule.Application`)

**Why it fails**: Each module's Application layer is independent. Cross-module queries/commands go through MediatR, never direct service calls.

**Fix pattern**:
- ❌ **WRONG**: `BookingModule.Application/Features/Bookings/CreateBookingHandler.cs` injects `IClubService` from `ClubModule.Application`
- ✅ **RIGHT**: CreateBookingHandler sends a MediatR query to `ClubModule` to fetch club data

**Code example**:
```csharp
// ❌ BookingModule.Application/Features/Bookings/CreateBookingHandler.cs
using ClubModule.Application.Services;  // VIOLATION
public class CreateBookingHandler : IRequestHandler<CreateBookingCommand, Guid>
{
    private readonly IClubService _clubService;  // Direct cross-module dependency
    
    public CreateBookingHandler(IClubService clubService) => _clubService = clubService;
    
    public async Task<Guid> Handle(CreateBookingCommand cmd, CancellationToken ct)
    {
        var club = await _clubService.GetClubAsync(cmd.ClubId);  // Direct call
        // ...
    }
}

// ✅ Refactored (use MediatR)
public class CreateBookingHandler : IRequestHandler<CreateBookingCommand, Guid>
{
    private readonly IMediator _mediator;
    
    public CreateBookingHandler(IMediator mediator) => _mediator = mediator;
    
    public async Task<Guid> Handle(CreateBookingCommand cmd, CancellationToken ct)
    {
        // Send query to ClubModule
        var club = await _mediator.Send(new GetClubByIdQuery(cmd.ClubId), ct);
        
        if (club == null)
            throw new ClubNotFoundException();
        
        // Proceed with booking creation
        // ...
    }
}

// ClubModule exposes this query
// ClubModule.Application/Features/Clubs/GetClubByIdQuery.cs
public class GetClubByIdQuery : IRequest<ClubDto>
{
    public Guid ClubId { get; set; }
    public GetClubByIdQuery(Guid clubId) => ClubId = clubId;
}
```

## Test Failure Summary Interpretation

When architecture tests fail, you'll see output like:

```
FAIL ClubModule.Domain should not reference BookingModule.Domain
FAIL BookingModule.Application should not reference ClubModule.Application
PASS All Domain projects isolation verified
```

**For each FAIL:**
1. Identify the **violation type** (rule 1, 2, or 3 above)
2. Find the **violating project reference** in the corresponding `.csproj` file
3. Apply the **fix pattern** above
4. Remove the `<ProjectReference>` from `.csproj` or refactor the code
5. Rerun: `dotnet test tests/JustGo.ArchitectureTests`

## Common Violation Scenarios

### Scenario A: "I need data from another module"

**Symptom**: You want to access another module's entity or service directly

**Solution**:
- Export a read-only DTO from the other module's Application layer
- Send a MediatR query to retrieve that DTO
- Never reference the other module's Domain or Application in your code

### Scenario B: "I'm using a shared utility class"

**Symptom**: You reference `JustGoAPI.Shared.Helpers.StringExtensions` or similar in Domain

**Solution**:
- Move the utility to your module's Domain (make it local to your bounded context)
- OR keep it in Shared but reference it only from Application/API layers, not Domain

### Scenario C: "I need to validate against another module's rules"

**Symptom**: Your Domain wants to validate using another module's validation logic

**Solution**:
- Domain performs only its own validation rules
- Cross-module validation happens at the Application layer after queries resolve
- Use FluentValidation in Application, not in Domain

## Workflow

### Before Commit

```bash
# Run full architecture test suite
dotnet test tests/JustGo.ArchitectureTests

# If failures, follow the fix patterns above
# Then verify locally:
dotnet build
dotnet test

# Then commit
```

### During Code Review

If a PR has architecture test failures:
1. Use this skill to diagnose the violation
2. Point reviewer to the relevant fix pattern
3. Explain why the refactored approach is better (DDD isolation, testability, reusability)

## Module Structure Reminder

Each of the 11 business modules follows this structure:

```
src/Modules/{ModuleName}/
├── {ModuleName}.Domain/          # ← Dependency-free (can only reference own files)
├── {ModuleName}.Application/     # ← Can reference own Domain, never sibling Application
├── {ModuleName}.Infrastructure/  # ← Implementation details
└── {ModuleName}.API/             # ← Controllers + ServiceRegistration
```

**Shared infrastructure** (`src/JustGo.Authentication/`, `src/JustGoAPI.Shared/`) provides cross-module utilities.

## See Also

- [Full Architecture Docs](../../docs/DDD-Modular-Monolith-Architecture.md)
- [MediatR Request/Handler Pattern](../../AGENTS.md#cqrs-mediatr)
- [Module Boundary Rules](../../AGENTS.md#module-boundary-rules-enforced-by-architecture-tests)
