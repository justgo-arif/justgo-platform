---
applyTo: "src/Modules/**/*.Application/**"
---

# Application Layer Conventions

Every feature lives under `{Module}.Application/Features/{Area}/{V1|V2}/Commands|Queries/{FeatureName}/`.

## Required files per feature

| File | Naming |
|------|--------|
| Request | `{Feature}Command.cs` / `{Feature}Query.cs` |
| Handler | `{Feature}Handler.cs` |
| Validator | `{Feature}Validator.cs` (if input exists) |
| DTO | in `DTOs/` folder if not already shared |

## Request

```csharp
public class MyCommand : IRequest<MyResponseDto>
{
    public Guid Id { get; set; }
    // properties set from controller — no business logic here
}
```

## Handler

```csharp
public class MyCommandHandler : IRequestHandler<MyCommand, MyResponseDto>
{
    private readonly LazyService<IWriteRepository<object>> _writeRepo;
    private readonly LazyService<IReadRepository<MyResponseDto>> _readRepo;

    public MyCommandHandler(
        LazyService<IWriteRepository<object>> writeRepo,
        LazyService<IReadRepository<MyResponseDto>> readRepo)
    {
        _writeRepo = writeRepo;
        _readRepo = readRepo;
    }

    public async Task<MyResponseDto> Handle(MyCommand request, CancellationToken ct)
    {
        // Use raw SQL — never EF LINQ
        await _writeRepo.Value.ExecuteAsync("spMyStoredProc", new { request.Id });
        // OR for raw SQL: commandType: "text"
        var result = await _readRepo.Value.GetAsync("SELECT ...", ct, new { request.Id }, commandType: "text");
        return result;
    }
}
```

## Validator

```csharp
public class MyCommandValidator : AbstractValidator<MyCommand>
{
    public MyCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
```

Auto-registered via `services.AddValidatorsFromAssembly(...)` — no manual DI needed.
