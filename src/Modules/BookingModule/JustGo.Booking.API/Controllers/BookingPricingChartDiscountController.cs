using Asp.Versioning;
using JustGo.Authentication.Helper.Attributes;
using JustGo.Authentication.Infrastructure.JwtAuthentication;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomErrors;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Booking.Application.DTOs.BookingClassesDTOs;
using JustGo.Booking.Application.Features.BookingClasses.Commands.AddPricingChartDiscount;
using JustGo.Booking.Application.Features.BookingClasses.Commands.DeletePricingChartDiscount;
using JustGo.Booking.Application.Features.BookingClasses.Commands.UpdatePricingChartDiscount;
using JustGo.Booking.Application.Features.BookingClasses.Commands.UpdatePricingChartDiscountStatus;
using JustGo.Booking.Application.Features.BookingClasses.Queries.GetPricingChartDiscountList;
using JustGo.Booking.Application.Features.BookingClasses.Queries.GetPricingChartDropdown;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace JustGo.Booking.API.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/booking-pricingchart-discount")]
[ApiController]
[Tags("Class Booking/PricingChartDiscount")]
[TenantFromHeader]
public class BookingPricingChartDiscountController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICustomError _error;
    private readonly IUtilityService _utilityService;
    public BookingPricingChartDiscountController(IMediator mediator, ICustomError error, IUtilityService utilityService)
    {
        _mediator = mediator;
        _error = error;
        _utilityService = utilityService;
    }

    [CustomAuthorize]
    [HttpGet("pricing-chart-dropdown")]
    [ResponseCache(Duration = 30, Location = ResponseCacheLocation.Client, VaryByHeader = "X-Tenant-Id")]
    public async Task<IActionResult> GetPricingChartDropdown([FromQuery] string ownerGuid, [FromQuery] string? searchTerm, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetPricingChartDropdownQuery(ownerGuid, searchTerm), cancellationToken);
        if (result is null || result.Count == 0)
        {
            _error.NotFound<object>("No pricing charts found.");
            return new EmptyResult();
        }
        return Ok(new ApiResponse<List<PricingChartDropdownDto>, object>(result));
    }

    [CustomAuthorize]
    [HttpPost("pricing-chart-discount")]
    public async Task<IActionResult> AddPricingChartDiscount([FromBody] AddPricingChartDiscountCommand command, CancellationToken cancellationToken)
    {
        var discountId = await _mediator.Send(command, cancellationToken);
        if (discountId <= 0)
        {
            _error.CustomValidation<object>("Failed to create discount.");
            return new EmptyResult();
        }
        return Ok(new ApiResponse<int, object>(discountId));
    }

    [CustomAuthorize]
    [HttpPut("pricing-chart-discount")]
    public async Task<IActionResult> UpdatePricingChartDiscount([FromBody] UpdatePricingChartDiscountCommand command, CancellationToken cancellationToken)
    {
        var affectedRows = await _mediator.Send(command, cancellationToken);
        if (affectedRows <= 0)
        {
            _error.CustomValidation<object>("Failed to update discount.");
            return new EmptyResult();
        }
        return Ok(new ApiResponse<int, object>(affectedRows));
    }

    [CustomAuthorize]
    [HttpPut("pricing-chart-discount-status")]
    public async Task<IActionResult> UpdatePricingChartDiscountStatus([FromBody] UpdatePricingChartDiscountStatusCommand command, CancellationToken cancellationToken)
    {
        var affectedRows = await _mediator.Send(command, cancellationToken);
        if (affectedRows <= 0)
        {
            _error.CustomValidation<object>("Failed to update discount status.");
            return new EmptyResult();
        }
        return Ok(new ApiResponse<int, object>(affectedRows));
    }

    [CustomAuthorize]
    [HttpDelete("pricing-chart-discount/{id:int}")]
    public async Task<IActionResult> DeletePricingChartDiscount(int id, CancellationToken cancellationToken)
    {
        var affectedRows = await _mediator.Send(new DeletePricingChartDiscountCommand(id), cancellationToken);
        if (affectedRows <= 0)
        {
            _error.CustomValidation<object>("Failed to delete discount.");
            return new EmptyResult();
        }
        return Ok(new ApiResponse<int, object>(affectedRows));
    }

    [CustomAuthorize]
    [HttpGet("pricing-chart-discount-list")]
    public async Task<IActionResult> GetPricingChartDiscountList([FromQuery] string? searchTerm, string ownerGuid, CancellationToken cancellationToken)
    {
        int ownerId = await _utilityService.GetOwnerIdByGuid(ownerGuid, cancellationToken);
        var result = await _mediator.Send(new GetPricingChartDiscountListQuery(searchTerm, ownerId), cancellationToken);

        if (result == null || result.Count == 0)
        {
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                return Ok(new ApiResponse<List<PricingChartDiscountListDto>, object>(
                    new List<PricingChartDiscountListDto>(),
                    $"No results found for search term '{searchTerm}'"
                ));
            }
            else
            {
                _error.NotFound<object>("No pricing chart discounts available. Please create some discounts first.");
                return new EmptyResult();
            }
        }

        return Ok(new ApiResponse<List<PricingChartDiscountListDto>, object>(result));
    }
}