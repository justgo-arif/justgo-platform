using Asp.Versioning;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Booking.Application.Features.ClassTerm.Commands.RemoveTermHoliday;
using JustGo.Booking.Application.Features.ClassTerm.Queries.GetTermRollingPeriods;
using JustGo.Booking.Application.Features.ClassTerm.Queries.GetTermTypes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace JustGo.Booking.API.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/class-terms")]
[ApiController]
[Tags("Class Booking/Terms")]
public class ClassTermController : ControllerBase
{
    private readonly IMediator _mediator;
    public ClassTermController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("get-term-lookup-data")]
    [ResponseCache(Duration = 600, Location = ResponseCacheLocation.Client)]
    public async Task<IActionResult> GetTermLookupData(CancellationToken cancellationToken)
    {
        var termTypesTask = _mediator.Send(new GetTermTypeQuery(), cancellationToken);
        var rollingPeriodsTask = _mediator.Send(new GetTermRollingPeriodQuery(), cancellationToken);

        await Task.WhenAll(termTypesTask, rollingPeriodsTask);

        var result = new
        {
            TermTypes = await termTypesTask,
            RollingPeriods = await rollingPeriodsTask
        };

        return Ok(new ApiResponse<object, object>(result));
    }
    [HttpPut("remove-term-holiday")]
    public async Task<IActionResult> RemoveJustGoBookingTermHoliday(int termHolidayId,CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new RemoveJustGoBookingTermHolidayCommand(termHolidayId), cancellationToken);
        return Ok(new ApiResponse<object, object>(result));
    }


}