using Asp.Versioning;
using JustGo.Authentication.Infrastructure.JwtAuthentication;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MobileApps.Application.Features.Class.V2.Queries.GetOccurrenceBookingCount;
using MobileApps.Application.Features.Club.Queries.GetClubList.V2;
using MobileApps.Application.Features.Club.V2.Query.GetClubEventBookingCount;
using MobileApps.Application.Features.Club.V2.Query.GetClubList;
using MobileApps.Application.Features.Club.V2.Query.GetClubListWithLazy;
using MobileApps.Domain.Entities.V2;
using MobileApps.Domain.Entities.V2.Clubs;


namespace MobileApps.API.Controllers.V2
{
    [ApiVersion("2.0")]
    [ApiVersion("3.0")]
    [Route("api/v{version:apiVersion}/clubs")]
    [ApiController]
    [Tags("Mobile Apps/Clubs")]
    public class ClubsController : ControllerBase
    {
        IMediator _mediator;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ClubsController(IMediator mediator, IHttpContextAccessor httpContextAccessor)
        {
            _mediator = mediator;
            _httpContextAccessor = httpContextAccessor;
        }

        [CustomAuthorize]
        [HttpPost("list")]
        public async Task<IActionResult> GetClubs(GetClubListQuery getClubListQuery, CancellationToken cancellationToken)
        {
            var resultData = await _mediator.Send(getClubListQuery, cancellationToken);

            if (resultData.Count == 0) return Ok(new ApiResponse<object, object>(null));
            var selectedIds = resultData.Select(c => new ClubEventWithClassFlagDto { DocId = c.DocId, SyncGuid = c.SyncGuid }).ToList();

            var flagDataList = await _mediator.Send(new ClubEventWithClassFlagQuery { ClubIds = selectedIds }, cancellationToken);

            var joinedData = from club in resultData
                             join flag in flagDataList on club.DocId equals flag.DocId
                             select new SwitcherClub
                             {
                                 DocId = club.DocId,
                                 SyncGuid = club.SyncGuid,
                                 Name = club.Name,
                                 Image = club.Image,
                                 IsEventExist = flag.IsExistEvent,
                                 IsClassExist = flag.IsExistClass,
                                 ImagePath = club.ImagePath,
                                 Reference = club.Reference,
                                 MerchantGuid = club.MerchantGuid,
                                 EntityType = club.EntityType
                             };


            return Ok(new ApiResponse<object, object>(joinedData));
        }
        [CustomAuthorize]
        [HttpGet("event-booking-count")]
        public async Task<IActionResult> GetClubTotalBookingCount(long clubDocId, DateTime? BookingDate, CancellationToken cancellationToken)
        {
            return Ok(new ApiResponse<object, object>(await _mediator.Send(new GetClubBookingCountQuery { ClubDocId = clubDocId }, cancellationToken)));
        }

        [CustomAuthorize]
        [HttpGet("class-booking-count/{clubGuid}")]
        public async Task<IActionResult> GetClassTotalBookingCount(Guid clubGuid, CancellationToken cancellationToken)
        {
            return Ok(new ApiResponse<object, object>(await _mediator.Send(new ClassBookingCountQuery { ClubSyncGuid = clubGuid }, cancellationToken)));
        }

        //test club list api 
        [CustomAuthorize]
        [HttpPost("all")]
        public async Task<IActionResult> GetLazyClubs(GetClubListLazyQuery getClubListQuery, CancellationToken cancellationToken)
        {
            var resultData = await _mediator.Send(getClubListQuery, cancellationToken);

            if (resultData.Count == 0) return Ok(new ApiResponse<object, object>(resultData));

           int totalCount = Convert.ToInt32(resultData.LastOrDefault()["TotalCount"]);
           int nextId = Convert.ToInt32(resultData.LastOrDefault()["RowNum"]);

            var selectedIds = resultData.Select(c => new ClubEventWithClassFlagDto { DocId = Convert.ToInt32(c["DocId"]), SyncGuid = c["SyncGuid"].ToString() }).ToList();

            var flagDataList = await _mediator.Send(new ClubEventWithClassFlagQuery { ClubIds = selectedIds }, cancellationToken);

            var joinedData = from club in resultData
                             join flag in flagDataList on Convert.ToInt32(club["DocId"]) equals flag.DocId
                             select new SwitcherLazyClub
                             {
                                 DocId = Convert.ToInt32(club["DocId"]),
                                 SyncGuid = club["SyncGuid"].ToString(),
                                 Name = club["Name"].ToString(),
                                 Image = club["Image"].ToString(),
                                 IsEventExist = flag.IsExistEvent,
                                 IsClassExist = flag.IsExistClass,
                                 ImagePath = club.ContainsKey("ImagePath") && club["ImagePath"] != null ? club["ImagePath"].ToString() : "",
                                 Reference = club.ContainsKey("Reference") && club["Reference"] != null ? club["Reference"].ToString() : "",
                                 MerchantGuid = club.ContainsKey("MerchantGuid") && club["MerchantGuid"] != null ? club["MerchantGuid"].ToString() : "",
                                 EntityType = club.ContainsKey("EntityType") && club["EntityType"] != null ? club["EntityType"].ToString() : ""
                             };


            return Ok(new ApiResponseWithCount<object, object>(joinedData, null, totalCount, nextId));
        }
    }
}