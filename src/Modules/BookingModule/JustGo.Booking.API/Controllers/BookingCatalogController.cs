using Asp.Versioning;
using JustGo.Authentication.Helper.Attributes;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomErrors;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Booking.Application.Features.BookingCatalogs.Queries.GetAgeGroupDescription;
using JustGo.Booking.Application.Features.BookingCatalogs.Queries.GetAgeGroups;
using JustGo.Booking.Application.Features.BookingCatalogs.Queries.GetBasicClubDetails;
using JustGo.Booking.Application.Features.BookingCatalogs.Queries.GetDisciplineDescription;
using JustGo.Booking.Application.Features.BookingCatalogs.Queries.GetDisciplines;
using JustGo.Booking.Application.Features.BookingCatalogs.Queries.GetFilterMetaData;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace JustGo.Booking.API.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/booking-catalogs")]
[ApiController]
[Tags("Class Booking/Catalogs")]
[AllowAnonymous]
[TenantFromHeader]
public class BookingCatalogController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICustomError _error;
    private readonly IUtilityService _utilityService;
    public BookingCatalogController(IMediator mediator, ICustomError error, IUtilityService utilityService)
    {
        _mediator = mediator;
        _error = error;
        _utilityService = utilityService;
    }

    [HttpGet("disciplines/{id:guid:required}")]
    [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Client, VaryByHeader = "X-Tenant-Id")]
    public async Task<IActionResult> GetDisciplinesBySyncGuid([FromRoute] Guid id, [FromQuery] Guid? webletGuid, CancellationToken cancellationToken)
    {
        if (id.Equals(Guid.Empty))
        {
            _error.CustomValidation<object>("Invalid request.");
            return new EmptyResult();
        }

        var result = await _mediator.Send(new GetDisciplinesBySyncGuidQuery(id, webletGuid), cancellationToken);

        if (result is null)
        {
            _error.NotFound<object>("No data found.");
            return new EmptyResult();
        }
        return Ok(new ApiResponse<object, object>(result));
    }

    [HttpGet("age-groups/{id:guid:required}")]
    [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Client, VaryByHeader = "X-Tenant-Id")]
    public async Task<IActionResult> GetAgeGroupsBySyncGuid([FromRoute] Guid id, [FromQuery] Guid? webletGuid, CancellationToken cancellationToken)
    {
        if (id.Equals(Guid.Empty))
        {
            _error.CustomValidation<object>("Invalid request.");
            return new EmptyResult();
        }

        var result = await _mediator.Send(new GetAgeGroupsBySyncGuidQuery(id, webletGuid), cancellationToken);
        if (result is null)
        {
            _error.NotFound<object>("No data found.");
            return new EmptyResult();
        }
        return Ok(new ApiResponse<object, object>(result));
    }

    [HttpGet("discipline-description/{id:guid:required}")]
    [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Client, VaryByHeader = "X-Tenant-Id")]
    public async Task<IActionResult> GetDisciplineDescription([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        if (id.Equals(Guid.Empty))
        {
            _error.CustomValidation<object>("Invalid request.");
            return new EmptyResult();
        }

        var result = await _mediator.Send(new GetDisciplineDescriptionQuery(id), cancellationToken);
        if (result is null)
        {
            _error.NotFound<object>("No data found.");
            return new EmptyResult();
        }
        return Ok(new ApiResponse<object, object>(result));
    }

    [HttpGet("age-group-description/{id:int:min(1)}")]
    [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Client, VaryByHeader = "X-Tenant-Id")]
    public async Task<IActionResult> GetAgeGroupDescription([FromRoute] int id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetAgeGroupDescriptionQuery(id), cancellationToken);
        
        if (result is null)
        {
            _error.NotFound<object>("No data found.");
            return new EmptyResult();
        }
        return Ok(new ApiResponse<object, object>(result));
    }

    [HttpGet("filter-metadata/{id:guid:required}")]
    [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Client, VaryByHeader = "X-Tenant-Id")]
    public async Task<IActionResult> GetFilterMetadata([FromRoute] Guid id, [FromQuery] Guid? webletGuid, CancellationToken cancellationToken)
    {
        if (id.Equals(Guid.Empty))
        {
            _error.CustomValidation<object>("Invalid request.");
            return new EmptyResult();
        }

        var result = await _mediator.Send(new GetFilterMetadataQuery(id, webletGuid), cancellationToken);
        if (result is null)
        {
            _error.NotFound<object>("No data found.");
            return new EmptyResult();
        }
        return Ok(new ApiResponse<object, object>(result));
    }

    [HttpGet("basic-club-details/{id:guid:required}")]
    [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Client, VaryByHeader = "X-Tenant-Id")]
    public async Task<IActionResult> GetBasicClubDetail([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        if (id.Equals(Guid.Empty))
        {
            _error.CustomValidation<object>("Invalid request.");
            return new EmptyResult();
        }

        var result = await _mediator.Send(new GetBasicClubDetailBySyncGuidQuery(id), cancellationToken);

        if (result is null)
        {
            _error.NotFound<object>("No data found.");
            return new EmptyResult();
        }
        return Ok(new ApiResponse<object, object>(result));
    }
}