using Asp.Versioning;
using JustGo.Authentication.Infrastructure.JwtAuthentication;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Membership.Application.Features.Memberships.Queries.GetFamilyByMemberDocId;
using JustGo.Membership.Application.Features.Memberships.Queries.GetLicenseDataCaptureItems;
using JustGo.Membership.Application.Features.Memberships.Queries.GetLicenses;
using JustGo.Membership.Application.Features.Memberships.Queries.GetMembersBasicDetailsQuery;
using JustGo.Membership.Application.Features.Memberships.Queries.GetMerchandiseItems;
using JustGo.Membership.Application.Features.Memberships.Queries.GetMyClubsBySyncGuid;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace JustGo.Membership.API.Controllers.V1
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/membershipspurchase")]
    [ApiController]
    [Tags("Membership/MembershipsPurchase")]
    public class MembershipsPurchaseController : ControllerBase
    {
        readonly IMediator _mediator;

        public MembershipsPurchaseController(IMediator mediator)
        {
            _mediator = mediator;
        }


        [CustomAuthorize]
        [HttpGet("get-family/{memberDocId}")]
        public async Task<IActionResult> GetFamilyByMember(int memberDocId, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetFamilyByMemberQuery(memberDocId), cancellationToken);
            return Ok(new ApiResponse<object, object>(result));
        }

        [CustomAuthorize]
        [HttpPost("get-members-details")]
        public async Task<IActionResult> GetMembersBasicDetails([FromBody] List<int> memberDocIds, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetMembersBasicDetailsQuery(memberDocIds), cancellationToken);
            return Ok(new ApiResponse<object, object>(result));
        }

        [CustomAuthorize]
        [HttpGet("get-license-data-capture-items/{licenseDocId}")]
        public async Task<IActionResult> GetLicenseDataCaptureItems(int licenseDocId, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetLicenseDataCaptureItemsQuery(licenseDocId), cancellationToken);
            return Ok(new ApiResponse<object, object>(result));
        }

        [CustomAuthorize]
        [HttpGet("get-my-clubs/{id:guid}")]
        public async Task<IActionResult> GetMyClubs(Guid id, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetMyClubsBySyncGuidQuery(id), cancellationToken);
            return Ok(new ApiResponse<object, object>(result));
        }

        [CustomAuthorize]
        [HttpGet("get-licenses/{id:guid}/{type}/{licenseTypeField}")]
        public async Task<IActionResult> GetLicenses(Guid id, string type, int licenseTypeField, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetLicensesQuery(id, type, licenseTypeField), cancellationToken);
            return Ok(new ApiResponse<object, object>(result));
        }


        [CustomAuthorize]
        [HttpPost("get-merchandise-items")]
        public async Task<IActionResult> GetMerchandiseItems([FromBody] List<string> ids, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetMerchandiseItemsQuery(ids), cancellationToken);
            return Ok(new ApiResponse<object, object>(result));
        }

    }
}
