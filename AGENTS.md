# JustGo Platform — Agent Instructions

Sports booking & venue management platform. DDD modular monolith (.NET 9, C#).

## Build & Test

```bash
dotnet restore && dotnet build
dotnet run --project src/JustGoAPI.API          # http://localhost:5152, Swagger at /swagger
dotnet test                                      # all tests
dotnet test tests/JustGo.ArchitectureTests       # run before committing — enforces module boundaries
dotnet test src/Modules/<Module>/tests/<Suite>   # single module tests
```

> **Always run architecture tests before committing.** They catch cross-module dependency violations.

## Architecture

See [docs/DDD-Modular-Monolith-Architecture.md](docs/DDD-Modular-Monolith-Architecture.md) for the full design.

**11 business modules** in `src/Modules/`, each strictly layered:

```
{Module}/
├── {Module}.Domain/          # Entities, aggregates, value objects — zero external deps
├── {Module}.Application/     # CQRS handlers (MediatR), validators, mapping profiles
├── {Module}.Infrastructure/  # DbContext, adapters
└── {Module}.API/             # Controllers + ServiceRegistration extension
```

**Shared infrastructure** (`src/JustGo.Authentication/`): generic repositories, JWT auth, ABAC, caching, pagination, `LazyService<T>`, `TenantContextManager`, `DatabaseSwitcher`, `DatabaseProvider`.

**Shared DTOs / utilities** (`src/JustGoAPI.Shared/`): Mapster config base, common DTOs.

**Composition root** (`src/JustGoAPI.API/Program.cs`): wires all modules via `Add{Module}Services()`.

## Module Boundary Rules (enforced by architecture tests)

- **Domain** must not reference any sibling module project or `JustGoAPI.Shared`
- **Application** must not reference sibling Application projects
- Cross-module communication goes through **MediatR only**, never direct DI

## Key Patterns

### CQRS (MediatR)

Every use case = one `IRequest<T>` + one `IRequestHandler<TRequest, TResponse>`.

```csharp
// Query
public class GetClubsQuery : IRequest<KeysetPagedResult<ClubDto>> { ... }

// Handler in {Module}.Application/Features/{Area}/
public class GetClubsHandler : IRequestHandler<GetClubsQuery, KeysetPagedResult<ClubDto>>
{
    private readonly LazyService<IReadRepository<ClubDto>> _readRepository;
    public async Task<KeysetPagedResult<ClubDto>> Handle(GetClubsQuery request, CancellationToken ct)
    {
        var results = await _readRepository.Value.GetListAsync(
            "SELECT ... FROM ...",   // raw parameterized SQL
            ct,
            new { request.UserSyncId },
            commandType: "text");
        return results;
    }
}
```

### Repository Pattern

**Never use EF LINQ** — pass raw parameterized SQL to Dapper-backed generic repositories.

```csharp
// Read
IReadRepository<T>  →  GetListAsync / GetAsync / GetSingleAsync / GetMultipleQueryAsync
// Write
IWriteRepository<T>  →  ExecuteAsync / ExecuteMultipleAsync / ExecuteScalarAsync

// commandType: "sp" (default) = stored procedure | "text" = raw SQL
await _repo.Value.GetListAsync(sql, ct, new { Param = value }, commandType: "text");
await _writeRepo.Value.ExecuteAsync("spName", new { Param = value }); // stored proc
```

Always use `LazyService<IReadRepository<T>>` / `LazyService<IWriteRepository<T>>` in handler constructors — not the repository interface directly.

### Multi-Tenancy

`AsyncLocal`-based context flows through the async chain:

```csharp
TenantContextManager.SetTenantId(tenantId);
DatabaseSwitcher.UseTenantDatabase();   // or .UseCentralDatabase()
// IDatabaseProvider resolves the connection string automatically
```

- **Tenant database** (`DatabaseType.Tenant`): application data per tenant
- **Central database** (`DatabaseType.Central`): tenant registry with encrypted credentials
- Credentials stored encrypted; `IUtilityService.DecryptData()` decrypts at runtime

### Controllers

```csharp
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/resource")]
[ApiController]
[Tags("Module/Area")]
public class MyController : ControllerBase
{
    readonly IMediator _mediator;

    [CustomAuthorize]   // ABAC — use [AllowAnonymous] only for unauthenticated endpoints
    [HttpGet("{id:guid:required}")]
    [ProducesResponseType(typeof(ApiResponse<MyDto, object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetMyQuery(id), ct);
        return Ok(new ApiResponse<MyDto, object>(result));
    }
}
```

- Always wrap responses in `ApiResponse<TData, TError>`.
- Use `ICustomError` for domain-specific error responses (not-found, conflict, etc.).
- Route constraints: `{id:guid:required}` — be explicit.

### Module DI Registration

Every module's `{Module}.API/{Module}ServiceRegistration.cs`:

```csharp
public static IServiceCollection Add{Module}Services(this IServiceCollection services)
{
    services.AddMediatR(cfg => {
        cfg.RegisterServicesFromAssembly(Assembly.Load("{Module}.Application"));
        cfg.AddOpenBehavior(typeof(RequestResponseLoggingBehavior<,>));
        cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
    });
    services.AddValidatorsFromAssembly(Assembly.Load("{Module}.Application"));
    // module-specific services...
    return services;
}
```

Then call `builder.Services.Add{Module}Services()` in `Program.cs`.

### Validation

FluentValidation, auto-discovered from the Application assembly. Runs before the handler via `ValidationBehavior<,>`.

```csharp
public class MyCommandValidator : AbstractValidator<MyCommand>
{
    public MyCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotNull().MaximumLength(100);
    }
}
```

### Mapping (Mapster)

Convention: `MyEntity` → `MyEntityDto` (auto-mapped by name). Module profiles inherit `CustomMapsterProfile`:

```csharp
public class MyMappingProfile : CustomMapsterProfile
{
    public override void Register(TypeAdapterConfig config)
        => CreateAutoMaps(config,
               Assembly.Load("{Module}.Domain"),
               Assembly.Load("{Module}.Application"));
}
```

For custom mappings: `config.NewConfig<Source, Dest>().Map(dest => dest.Foo, src => src.Bar)`.

## Testing

- **Framework**: xUnit with FluentAssertions
- **Mocking**: Moq — use `LazyServiceMockHelper.MockLazyService()` to wrap mocked repos
- **Location**: `src/Modules/{Module}/tests/{Module}.UnitTests/`
- **Architecture tests**: `tests/JustGo.ArchitectureTests/` — XUnit + reflection on `.csproj` XML

```csharp
[Fact]
public async Task Handle_ShouldReturnData_WhenExists()
{
    // Arrange
    var repoMock = new Mock<IReadRepository<MyDto>>();
    var lazyRepo = LazyServiceMockHelper.MockLazyService(repoMock.Object);
    repoMock.Setup(r => r.GetListAsync(It.IsAny<string>(), It.IsAny<CancellationToken>(),
        It.IsAny<object>(), null, "text"))
        .ReturnsAsync(new List<MyDto> { new() { ... } });

    var handler = new MyHandler(lazyRepo);

    // Act
    var result = await handler.Handle(new MyQuery(), CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
}
```

For commands with transactions, mock `IUnitOfWork.BeginTransactionAsync()` and verify `CommitAsync` was called.

## Logging

Serilog with custom sinks. Use `ILogger<T>` from DI — do not instantiate `Log.*` directly in handlers.

Custom sinks: `EventSink` (domain events), `ExceptionSink` (errors), `AuditSink` (audit trail).

## Common Pitfalls

- **Don't use `commandType: "sp"` for raw SQL** — the default is SP. Pass `commandType: "text"` explicitly for inline SQL.
- **Don't inject `IReadRepository<T>` directly** — always use `LazyService<IReadRepository<T>>`.
- **Don't reference sibling modules** — architecture tests will fail. Use MediatR requests instead.
- **Don't add Domain→Shared references** — Domain is dependency-free by design.
- **Don't use AutoMapper** — this project uses Mapster.
- **Multi-tenancy context is `AsyncLocal`** — set it in middleware/filters, not in handlers.
