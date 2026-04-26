---
applyTo: "src/Modules/**/*.API/Controllers/**"
---

# Controller Conventions

```csharp
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/resource")]
[ApiController]
[Tags("Module/Area")]                  // groups endpoints in Swagger
public class MyController : ControllerBase
{
    readonly IMediator _mediator;
    private readonly ICustomError _error;

    public MyController(IMediator mediator, ICustomError error)
    {
        _mediator = mediator;
        _error = error;
    }

    [CustomAuthorize]                  // ABAC; use [AllowAnonymous] only for public endpoints
    [HttpGet("{id:guid:required}")]
    [ProducesResponseType(typeof(ApiResponse<MyDto, object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetMyQuery(id), ct);
        if (result is null)
        {
            _error.NotFound<object>("Record not found.");
            return new EmptyResult();
        }
        return Ok(new ApiResponse<MyDto, object>(result));
    }

    [CustomAuthorize]
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<MyDto, object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Create([FromBody] CreateMyCommand request, CancellationToken ct)
    {
        var result = await _mediator.Send(request, ct);
        return Ok(new ApiResponse<MyDto, object>(result));
    }
}
```

- All responses wrapped in `ApiResponse<TData, TError>`.
- Always pass `CancellationToken ct` to `_mediator.Send()`.
- For multi-version controllers: `[ApiVersion("1.0")] [ApiVersion("2.0")]`.
- Map commands/queries directly from `[FromBody]` where the request object is the command/query.
