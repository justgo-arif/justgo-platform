using System.Data;
using Asp.Versioning;
using JustGo.Authentication.Infrastructure.JwtAuthentication;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MobileApps.Application.Features.Class.V2.Command.BulkAttendanceUpdate;
using MobileApps.Application.Features.Class.V2.Command.SingleAttendanceUpdate;
using MobileApps.Application.Features.Class.V2.Command.SingleNoteDelete;
using MobileApps.Application.Features.Class.V2.Command.SingleNoteUpdate;
using MobileApps.Application.Features.Class.V2.Queries.GetAttendeeNoteList;
using MobileApps.Application.Features.Class.V2.Queries.GetAttendeeOccurrenceNote;
using MobileApps.Application.Features.Class.V2.Queries.GetClassBookingList;
using MobileApps.Application.Features.Class.V2.Queries.GetClassSessionList;
using MobileApps.Application.Features.Class.V2.Queries.GetMemberSessionLicenses;
using MobileApps.Application.Features.Class.V2.Queries.GetOccurrenceAttendeeList;
using MobileApps.Application.Features.Class.V2.Queries.GetOccurrenceBookingCount;
using MobileApps.Application.Features.Class.V2.Queries.GetSessionEligibilityRules;
using MobileApps.Application.Features.Class.V2.Queries.GetSessionLicensesRules;
using MobileApps.Application.Features.Class.V2.Queries.GetSessionOccurrenceList;
using MobileApps.Application.Features.Class.V2.Queries.GetSessionPaymentRule;
using MobileApps.Application.Features.Class.V2.Queries.GetSessionsDaysOfWeekList;
using MobileApps.Application.Features.Class.V2.Queries.GetSessionTicketList;
using MobileApps.Application.Features.FieldManagement.Queries.FieldManagementQueryModel;
using MobileApps.Application.Features.FieldManagement.Queries.GetEntityExtensionSchema;
using MobileApps.Application.Features.FieldManagement.Queries.GetFieldManagementModelSchema;
using MobileApps.Application.Features.FieldManagement.Queries.GetFieldManagementMVData;
using MobileApps.Application.Features.FieldManagement.Queries.GetFMSystemFormCollection;
using MobileApps.Application.Features.Members.Queries.GetMemberByMemberDocId;
using MobileApps.Application.Features.User.V2.GetMemberDetails;
using MobileApps.Domain.Entities.V2.Classes;
using MobileApps.Domain.Entities.V2.Members;
using Newtonsoft.Json;

namespace MobileApps.API.Controllers.V2
{
    [ApiVersion("2.0")]
    [Route("api/v{version:apiVersion}/classes")]
    [ApiController]
    [Tags("Mobile Apps/Classes")]
    public class ClassesController : ControllerBase
    {
        IMediator _mediator;
        private readonly IHttpContextAccessor _httpContextAccessor;
        public ClassesController(IMediator mediator, IHttpContextAccessor httpContextAccessor)
        {
            _mediator = mediator;
            _httpContextAccessor = httpContextAccessor;
        }


        [CustomAuthorize]
        [MapToApiVersion("2.0")]
        [HttpPost("list")]
        public async Task<IActionResult> GetClassListAsync(GetClassListQuery query, CancellationToken cancellationToken)
        {

            if (query?.ClubSyncGuid?.Length == 0)
                return Ok(new ApiResponse<object, object>(null, 200, "Invalid Club Guid"));

            return Ok(new ApiResponse<object, object>(await _mediator.Send(query, cancellationToken)));
        }

        [CustomAuthorize]
        [MapToApiVersion("2.0")]
        [HttpPost("sessions")]
        public async Task<IActionResult> GetClassSessionsAsync(GetClassSessionQuery sessionQuery, CancellationToken cancellationToken)
        {
            return Ok(new ApiResponse<object, object>(await _mediator.Send(sessionQuery, cancellationToken)));
        }

        [CustomAuthorize]
        [MapToApiVersion("2.0")]
        [HttpPost("session-occurrences/{id}")]
        public async Task<IActionResult> GetSessionOccurrenceListAsync(int id, CancellationToken cancellationToken)
        {
            //getting recurring event occurrence booking date
            var occurrenceList = await _mediator.Send(new GetSessionOccurrenceListQuery { SessionId = id }, cancellationToken);
            foreach (var occurrence in occurrenceList)
            {
                var bookingCount = await _mediator.Send(new GetOccurrenceBookingCountQuery { OccurrenceId = Convert.ToInt32(occurrence["OccurrenceId"]) }, cancellationToken);
                occurrence.Add("BookingCount", bookingCount);
            }
            return Ok(new ApiResponse<object, object>(occurrenceList));
        }


        [CustomAuthorize]
        [MapToApiVersion("2.0")]
        [HttpPost("occurrence-booking-list")]
        public async Task<IActionResult> GetOccurrenceBookingListAsync(GetOccurrenceAttendeeListQuery queryParam, CancellationToken cancellationToken)
        {
            //getting recurring event occurrence booking date
            return Ok(new ApiResponse<object, object>(await _mediator.Send(queryParam, cancellationToken)));
        }



        [CustomAuthorize]
        [MapToApiVersion("2.0")]
        [HttpPost("bulk-attendance-update")]
        public async Task<IActionResult> UpdateBookingCheckedStatus(BulkAttendanceUpdateCommand command, CancellationToken cancellationToken)
        {

            return Ok(new ApiResponse<object, object>(await _mediator.Send(command, cancellationToken)));
        }


        [CustomAuthorize]
        [MapToApiVersion("2.0")]
        [HttpPost("single-attendance-update")]
        public async Task<IActionResult> SetAttendanceForOccurence(SingleAttendanceUpdateCommand command, CancellationToken cancellationToken)
        {
            return Ok(new ApiResponse<object, object>(await _mediator.Send(command, cancellationToken)));
        }


        [CustomAuthorize]
        [MapToApiVersion("2.0")]
        [HttpPost("ticket-list/{sessionId}")]
        public async Task<IActionResult> GetSessionTicketList(int sessionId, CancellationToken cancellationToken)
        {
            return Ok(new ApiResponse<object, object>(await _mediator.Send(new GetSessionTicketListQuery { SessionId = sessionId }, cancellationToken)));
        }

        [CustomAuthorize]
        [MapToApiVersion("2.0")]
        [HttpGet("attendance-status-list")]
        public async Task<IActionResult> GetAttendanceStatusList(CancellationToken cancellationToken)
        {
            var attendanceStatuses = new[]{
                new{Id=1,Status = "Attended",Color = "#C8E6C9" /*Light Green*/},
                new{Id=2,Status = "Away",Color = "#FFF9C4" /* light yellow*/},
                new{Id=3,Status = "Injured",Color = "#FFCDD2"  /*Light Pink*/},
                new{Id=4,Status = "No Show",Color = "#F8BBD0" /*Soft Pink*/},
                new{Id=5,Status = "Attended Late",Color = "#A5D6A7" /*Slightly darker Green"*/},
                new{Id=6,Status = "Pending",Color = "#455a64" /*Slightly darker Green"*/}
            };

            return Ok(new ApiResponse<object, object>(attendanceStatuses));
        }



        [CustomAuthorize]
        [MapToApiVersion("2.0")]
        [HttpPost("booking-count")]
        public async Task<IActionResult> GetBookingCount(GetOccurrenceBookingCountQuery countQuery, CancellationToken cancellationToken)
        {
            return Ok(new ApiResponse<object, object>(await _mediator.Send(countQuery, cancellationToken)));
        }



        [CustomAuthorize]
        [MapToApiVersion("2.0")]
        [HttpPost("session-days")]
        public async Task<IActionResult> GetSessionsDaysOfWeekList(SessionsDaysOfWeekListQuery queryParam, CancellationToken cancellationToken)
        {
            return Ok(new ApiResponse<object, object>(await _mediator.Send(queryParam, cancellationToken)));
        }


        [CustomAuthorize]
        [MapToApiVersion("2.0")]
        [HttpPost("member-details")]
        public async Task<IActionResult> GetClassBookingMemberInfo(ClassBookingInfoQuery queryParam, CancellationToken cancellationToken)
        {
            var dataList = new Dictionary<string, object>();
            //user customized data
            var userData = await _mediator.Send(new GetUserByUserIdQuery(queryParam.MemberDocId));

            if (userData == null) return Ok(new ApiResponse<object, object>(null, null, 200, "Invalid User"));
            //Member with emergency details
            var details = await _mediator.Send(new MemberDetailsQuery { MemberDocId = userData.MemberDocId });
            var memberDetails = ConvertToModel(details);
            memberDetails.PersonalInfo.ProfilePicURL = userData.ProfilePicURL;

            //Get member Eligibility
            var eligibilityData = await _mediator.Send(new SessionEligibilityRulesQuery { ProductId = queryParam.ProductId });

            //Member Session Membership details
            var membershipDetails = await _mediator.Send(new MemberSessionLicenseQuery { SessionId = queryParam.SessionId, MemberDocId = userData.MemberDocId });

            //Member Session Payment details
            var memberPayment = await _mediator.Send(new MemberSessionPaymentQuery { SessionId = queryParam.SessionId, AttendeeId = queryParam.AttendeeId, ProductId = queryParam.ProductId });

            //Member Session Payment details
            var sessionBookingDetails = await _mediator.Send(new MemberSessionBookingsQuery { SessionId = queryParam.SessionId, MemberDocId = userData.MemberDocId });


            dataList.Add("memberDetails", memberDetails);
            dataList.Add("membership", membershipDetails);
            dataList.Add("payment", memberPayment);
            dataList.Add("sessionBookingDetails", sessionBookingDetails);
        


            return Ok(new ApiResponse<object, object>(dataList));
        }


        [CustomAuthorize]
        [MapToApiVersion("2.0")]
        [HttpPost("booking-details")]
        public async Task<IActionResult> BookingFieldManagement(FieldManagenetBookingDetailsQuery queryParam, CancellationToken cancellationToken)
        {
            //Booking Additional Details (FM)
            var dataList = new Dictionary<string, object>();
            var ItemIds = new List<int>();

            var formSchemaList = await _mediator.Send(new BookingFMQuery { SessionId = queryParam.SessionId });

            if (formSchemaList.Count() > 0) ItemIds = formSchemaList.Select(m => m.ItemId).ToList();

            if (ItemIds.Count() > 0)
            {
                dataList = await GetFMSchemaAndData(ItemIds, queryParam.MemberDocId);
            }
            return Ok(new ApiResponse<object, object>(dataList));
        }

        [CustomAuthorize]
        [MapToApiVersion("2.0")]
        [HttpPost("additional-details")]
        public async Task<IActionResult> FieldManagement(FieldManagementQuery queryParam, CancellationToken cancellationToken)
        {
            //Member Additional Details (FM)
            var dataList = new Dictionary<string, object>();
            var ItemIds = new List<int>();

            var formSchemaList = await _mediator.Send(new FieldManagementMemberFormQuery { Entity = "NGB", UserId = queryParam.UserId, ItemKey = "Class.Member_Overview_Form_for_class" });

            if (formSchemaList!=null &&formSchemaList.Count() > 0) ItemIds = formSchemaList.Select(m => m.TabId).ToList();

            if (ItemIds.Count() > 0)
            {
                dataList = await GetFMSchemaAndData(ItemIds, queryParam.MemberDocId);
            }
            return Ok(new ApiResponse<object, object>(dataList));
        }


        [CustomAuthorize]
        [MapToApiVersion("2.0")]
        [HttpPost("note-list")]
        public async Task<IActionResult> GetNoteList(NoteListQuery queryParam, CancellationToken cancellationToken)
        {
            return Ok(new ApiResponse<object, object>(await _mediator.Send(queryParam, cancellationToken)));
        }


        [CustomAuthorize]
        [MapToApiVersion("2.0")]
        [HttpPost("occurrence-note")]
        public async Task<IActionResult> GetOccurrenceNote(OccurrenceNoteQuery queryParam, CancellationToken cancellationToken)
        {
            return Ok(new ApiResponse<object, object>(await _mediator.Send(queryParam, cancellationToken)));
        }

        [CustomAuthorize]
        [MapToApiVersion("2.0")]
        [HttpPut("note-update")]
        public async Task<IActionResult> UpdateNote(SingleNoteUpdateCommand queryParam, CancellationToken cancellationToken)
        {
            return Ok(new ApiResponse<object, object>(await _mediator.Send(queryParam, cancellationToken)));
        }

        [CustomAuthorize]
        [MapToApiVersion("2.0")]
        [HttpDelete("note-delete")]
        public async Task<IActionResult> DeleteNote(SingleNoteDeleteCommand queryParam, CancellationToken cancellationToken)
        {
            return Ok(new ApiResponse<object, object>(await _mediator.Send(queryParam, cancellationToken)));
        }

        [CustomAuthorize]
        [MapToApiVersion("2.0")]
        [HttpPost("eligibility")]
        public async Task<IActionResult> CheckEligibility(EligibilityQueryParam queryParam, CancellationToken cancellationToken)
        {
      
            //Member with emergency details
            var details = await _mediator.Send(new MemberDetailsQuery { MemberDocId = queryParam.MemberDocId });
            if(details==null || details.Count==0) return Ok(new ApiResponse<object,object>(null));
            var memberDetails = ConvertToModel(details);
            

            //Get member Eligibility
            var eligibilityData = await _mediator.Send(new SessionEligibilityRulesQuery { ProductId = queryParam.ProductId,UserId= memberDetails.PersonalInfo.UserId, MemberDocId = queryParam.MemberDocId });
            //if (eligibilityData == null) return Ok(new ApiResponse<object>(null)); 

            var paymentData = await _mediator.Send(new SessionPaymentRulesQuery { ProductId = queryParam.ProductId,AttendeeId= queryParam.AttendeeId,OccurrenceId=queryParam.OccurrenceId });

            //Member Session Membership details
            var membershipDetails = await _mediator.Send(new SessionLicensesRulesQuery { OccurrenceId = queryParam.OccurrenceId, MemberDocId = queryParam.MemberDocId });

            return Ok(new ApiResponse<object, object>(EligibilityMapping.CombinedEligibility(memberDetails.PersonalInfo, eligibilityData, paymentData, membershipDetails)));
        }

        #region private 
        private MemberDetails ConvertToModel(IDictionary<string, object> details)
        {
            var modelResult = JsonConvert.DeserializeObject<IDictionary<string, object>>(JsonConvert.SerializeObject(details));

            modelResult.TryGetValue("PersonalInfo", out var personalInfoObj);
            modelResult.TryGetValue("EmergencyContacts", out var emergencyContactsObj);

            var personalInfo = personalInfoObj != null
                ? JsonConvert.DeserializeObject<PersonalInfo>(personalInfoObj.ToString())
                : null;

            var emergencyContacts = emergencyContactsObj != null
                ? JsonConvert.DeserializeObject<List<EmergencyContact>>(emergencyContactsObj.ToString())
                : new List<EmergencyContact>();
            //// Wrap into your final model
            return new MemberDetails
            {
                PersonalInfo = personalInfo,
                EmergencyContacts = emergencyContacts
            };
        }
        private async Task<Dictionary<string, object>> GetFMSchemaAndData(List<int> ItemIds, int MemberDocId)
        {
            var dataList = new Dictionary<string, object>();
            var schemaCoreDataList = await _mediator.Send(new EntityExtensionSchemaQuery { ItemIds = ItemIds });
            var schemaWithData = new List<Dictionary<string, object>>();

            foreach (var schemaCore in schemaCoreDataList)
            {
                var additionalData = schemaCore != null
                    ? await _mediator.Send(new FieldManagementModelDataQuery { MemberDocId = MemberDocId, SchemaCore = schemaCore })
                    : null;



                var additionalDetailsSchema = schemaCore != null
                  ? await _mediator.Send(new FieldManagementModelSchemaQuery { ExId = schemaCore.ExId, EntityType = "Ngb", ItemId = schemaCore.ItemId })
                  : null;

                var obj = new Dictionary<string, object>
                {
                    { "schema", additionalDetailsSchema },
                    { "schemaData", additionalData }
                };

                schemaWithData.Add(obj);
            }

            dataList["AdditionalDetails"] = schemaWithData;
            return dataList;
        }

    

        #endregion
    }
}
