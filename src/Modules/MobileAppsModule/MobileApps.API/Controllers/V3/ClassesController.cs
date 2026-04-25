using Asp.Versioning;
using JustGo.Authentication.Infrastructure.JwtAuthentication;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MobileApps.Application.Features.Class.V3.Command.BulkAttendanceUpdate;
using MobileApps.Application.Features.Class.V3.Command.ClassAttendanceUpdate;
using MobileApps.Application.Features.Class.V3.Command.ClassNoteDelete;
using MobileApps.Application.Features.Class.V3.Command.ClassNoteUpdate;
using MobileApps.Application.Features.Class.V3.Command.MemberNoteDelete;
using MobileApps.Application.Features.Class.V3.Command.MemberNoteUpsert;
using MobileApps.Application.Features.Class.V3.Queries;
using MobileApps.Application.Features.Class.V3.Queries.GetMemberNoteCategoryList;
using MobileApps.Application.Features.Class.V3.Queries.GetMultipleOccurrenceBookingCount;
using MobileApps.Application.Features.FieldManagement.Queries.FieldManagementQueryModel;
using MobileApps.Application.Features.FieldManagement.Queries.GetEntityExtensionSchema;
using MobileApps.Application.Features.FieldManagement.Queries.GetFieldManagementModelSchema;
using MobileApps.Application.Features.FieldManagement.Queries.GetFieldManagementMVData;
using MobileApps.Application.Features.FieldManagement.Queries.GetFMSystemFormCollection;
using MobileApps.Application.Features.Members.Queries.GetMemberByMemberDocId;
using MobileApps.Application.Features.User.V2.GetMemberDetails;
using MobileApps.Domain.Entities.V2.Members;
using MobileApps.Domain.Entities.V3.Classes;
using Newtonsoft.Json;

namespace MobileApps.API.Controllers.V3
{
    [ApiVersion("3.0")]
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

        #region v3
        [CustomAuthorize]
        [HttpPost("session")]
        public async Task<IActionResult> GetClassListAsync(GetClassListQuery query, CancellationToken cancellationToken)
        {
            int totalCount = 0;
            int nextId = 0;
         
            if (!Guid.TryParse(query.ClubGuid, out Guid parsedGuid))
                return Ok(new ApiResponse<object, object>(null, 200, "Invalid Club Guid"));


            var result = await _mediator.Send(query, cancellationToken);

            if (result.Any())
            {
                totalCount = Convert.ToInt32(result.LastOrDefault()["TotalCount"]);
                nextId = Convert.ToInt32(result.LastOrDefault()["RowId"]);
                //// Keys to remove
                var keysToRemove = new[] { "TotalCount", "RowId" };

                // Fast iteration over the list
                for (int i = 0; i < result.ToList().Count; i++)
                {
                    // Remove each key if it exists
                    for (int j = 0; j < keysToRemove.Length; j++)
                    {

                        result[i].Remove(keysToRemove[j]);
                    }
                }
            }


            return Ok(new ApiResponseWithCount<object, object>(result, null, totalCount, nextId, 200));
           
        }

        [CustomAuthorize]
        [HttpGet("age-group/{id:int}")]
        public async Task<IActionResult> GetClassAgeGroupListAsync(int id, CancellationToken cancellationToken)
        {
            if (id < 0) return Ok(new ApiResponse<object, object>(null, 200, "Invalid Club doc Id"));

            return Ok(new ApiResponse<object, object>(await _mediator.Send( new GetClassAgeGroupListQuery{ ClubDocId =id}, cancellationToken)));
        }
        [CustomAuthorize]
        [HttpGet("categories/{id:int}")]
        public async Task<IActionResult> GetCategoryList(int id, CancellationToken cancellationToken)
        {
            if (id <0) return Ok(new ApiResponse<object, object>(null, 200, "Invalid club doc id"));
            return Ok(new ApiResponse<object, object>(await _mediator.Send(new CategoryListQuery { ClubDocId = id }, cancellationToken)));
        }

        [CustomAuthorize]
        [HttpGet("gender")]

        public async Task<IActionResult> GetClassGenderListAsync(CancellationToken cancellationToken)
        {
            return Ok(new ApiResponse<object, object>(await _mediator.Send(new GenderListQuery(), cancellationToken)));
        }

        [CustomAuthorize]
        [HttpGet("coaches/{id:int}")]

        public async Task<IActionResult> GetCoachListAsync(int id, CancellationToken cancellationToken)
        {
            if (id < 0) return Ok(new ApiResponse<object, object>(null, 200, "Invalid club doc id"));
            return Ok(new ApiResponse<object, object>(await _mediator.Send(new CoachListQuery { ClubDocId = id }, cancellationToken)));
        }

        [CustomAuthorize]
        [HttpGet("product-types/{id:int}")]
        public async Task<IActionResult> GetSessionTicketList(int id, CancellationToken cancellationToken)
        {
            if (id < 0) return Ok(new ApiResponse<object, object>(null, 200, "Invalid club doc id"));
            return Ok(new ApiResponse<object, object>(await _mediator.Send(new PaymentTypeListQuery { ClubDocId = id }, cancellationToken)));
        }

        [CustomAuthorize]
        [HttpGet("session-days/{id:int}")]
        public async Task<IActionResult> GetSessionsDaysOfWeekList(int id, CancellationToken cancellationToken)
        {

            if (id < 0) return Ok(new ApiResponse<object, object>(null, 200, "Invalid club doc id"));
            return Ok(new ApiResponse<object, object>(await _mediator.Send(new SessionsDaysOfWeekListQuery { ClubDocId=id }, cancellationToken)));
        }

        [CustomAuthorize]
        [HttpGet("session-statistics/{occurrenceId:int}")]
        public async Task<IActionResult> GetBookingCount(int occurrenceId, CancellationToken cancellationToken)
        {
            if (occurrenceId == 0) return Ok(new ApiResponse<object, object>(null, 200, "Invalid occurrence id"));
            return Ok(new ApiResponse<object, object>(await _mediator.Send(new GetOccurrenceBookingCountQuery { OccurrenceId=occurrenceId }, cancellationToken)));
        }

        [CustomAuthorize]
        [HttpPost("multi-occurrence-statistics")]
        public async Task<IActionResult> GetMultipleOccurrenceBookingCount(MultipleOccurrenceBookingCountQuery queryParam, CancellationToken cancellationToken)
        {
            if (queryParam.OccurrenceIds.Count == 0) return Ok(new ApiResponse<object, object>(null, 200, "Invalid occurrence id's"));
            return Ok(new ApiResponse<object, object>(await _mediator.Send(queryParam, cancellationToken)));
        }

        [CustomAuthorize]
        [HttpPost("occurrence-booking-list")]
        public async Task<IActionResult> GetOccurrenceBookingListAsync(GetOccurrenceAttendeeListQuery queryParam, CancellationToken cancellationToken)
        {
            int totalCount = 0;
            int nextId = 0;
            if (queryParam.ClubId == null) queryParam.ClubId = -1;
            var result = await _mediator.Send(queryParam, cancellationToken);

            if (result.Any())
            {
                totalCount = (int)result.FirstOrDefault().TotalCount;
                nextId = (int)result.LastOrDefault().RowId;
               
            }
            return Ok(new ApiResponseWithCount<object, object>(result, null, totalCount, nextId, 200));

        }
        [CustomAuthorize]
        [HttpGet("color-group")]
        public async Task<IActionResult> GetClassColorGroupListAsync(CancellationToken cancellationToken)
        {
            return Ok(new ApiResponse<object, object>(await _mediator.Send(new ClassColorGroupListQuery(), cancellationToken)));
        }
        #endregion v3

        #region member Note task v3+v4
        [CustomAuthorize]
        [HttpPost("member-notes")]
        public async Task<IActionResult> GetNoteList(MemberNoteListQuery queryParam, CancellationToken cancellationToken)
        {
            return Ok(new ApiResponse<object, object>(await _mediator.Send(queryParam, cancellationToken)));
        }


        [CustomAuthorize]
        [HttpGet("member-note-category")]
        public async Task<IActionResult> GetOccurrenceNote(CancellationToken cancellationToken)
        {
            return Ok(new ApiResponse<object, object>(await _mediator.Send(new MemberNoteCategoryListQuery(), cancellationToken)));
        }

        [CustomAuthorize]
        [HttpPost("member-note-upsert")]
        public async Task<IActionResult> UpdateNote(MemberNoteUpsertCommand queryParam, CancellationToken cancellationToken)
        {
            return Ok(new ApiResponse<object, object>(await _mediator.Send(queryParam, cancellationToken)));
        }

        [CustomAuthorize]
        [HttpDelete("member-note-delete")]
        public async Task<IActionResult> DeleteNote(MemberNoteDeleteCommand queryParam, CancellationToken cancellationToken)
        {
            return Ok(new ApiResponse<object, object>(await _mediator.Send(queryParam, cancellationToken)));
        }


        [CustomAuthorize]
        [HttpPost("is-eligible")]
        public async Task<IActionResult> CheckExistingEligibility(EligibilityQueryParam queryParam, CancellationToken cancellationToken)
        {
            var result = new Dictionary<string, object>();
            queryParam.ProductId= queryParam.ProductId<1? 0 : queryParam.ProductId; 
            //Member with emergency details
            var details = await _mediator.Send(new MemberDetailsQuery { MemberDocId = queryParam.MemberDocId });
            if (details == null || details.Count == 0) return Ok(new ApiResponse<object, object>(null, 200, "No data found!"));
            var memberDetails = ConvertToModel(details);


            //Get member Eligibility
            var eligibilityData = await _mediator.Send(new SessionEligibilityRulesQuery { ProductId = queryParam.ProductId, UserId = memberDetails.PersonalInfo.UserId, MemberDocId = queryParam.MemberDocId });
            //if (eligibilityData == null) return Ok(new ApiResponse<object>(null)); 

            var paymentData = await _mediator.Send(new SessionPaymentRulesQuery { ProductId = queryParam.ProductId, AttendeeId = queryParam.AttendeeId, OccurrenceId = queryParam.OccurrenceId });

            //Member Session Membership details
            var membershipDetails = await _mediator.Send(new SessionLicensesRulesQuery { OccurrenceId = queryParam.OccurrenceId, MemberDocId = queryParam.MemberDocId });
            bool isEligible = EligibilityMapping.CombinedEligibilityValidation(memberDetails.PersonalInfo, eligibilityData, paymentData, membershipDetails);
            result.Add("isEligible", isEligible);

            return Ok(new ApiResponse<object, object>(result));
        }

        #endregion

        #region old code
        [CustomAuthorize]
        [HttpPost("sessions-list")]
        public async Task<IActionResult> GetClassSessionsAsync(GetClassSessionQuery sessionQuery, CancellationToken cancellationToken)
        {
            return Ok(new ApiResponse<object, object>(await _mediator.Send(sessionQuery, cancellationToken)));
        }

        [CustomAuthorize]
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
        [HttpPost("bulk-attendance-update")]
        public async Task<IActionResult> UpdateBookingCheckedStatus(BulkAttendanceUpdateCommand command, CancellationToken cancellationToken)
        {

            return Ok(new ApiResponse<object, object>(await _mediator.Send(command, cancellationToken)));
        }

        [CustomAuthorize]
        [HttpPost("single-attendance-update")]
        public async Task<IActionResult> SetAttendanceForOccurence(SingleAttendanceUpdateCommand command, CancellationToken cancellationToken)
        {

            var retult = await _mediator.Send(command, cancellationToken);
            if (retult.Count > 0)
            {
                string message = Convert.ToBoolean(retult["IsExecute"]) ?  "Request successful":"Failed to update attendee. Please try again.";
                return Ok(new ApiResponse<object, object>(retult,200, message));
            }
            return Ok(new ApiResponse<object, object>(await _mediator.Send(command, cancellationToken)));
        }

        [CustomAuthorize]
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


            //Member Session Payment details
            //var ticketInfo = await _mediator.Send(new MemberTicketDetailsQuery { SessionId = queryParam.SessionId, MemberId = queryParam.MemberDocId, UserId = userData.Userid });

            dataList.Add("memberDetails", memberDetails);
            dataList.Add("membership", membershipDetails);
            dataList.Add("payment", memberPayment);
            dataList.Add("sessionBookingDetails", sessionBookingDetails);
            // dataList.Add("eligibility", EligibilityMapping.CombinedEligibility(memberDetails.PersonalInfo, eligibilityData,null,true));


            return Ok(new ApiResponse<object, object>(dataList));
        }

        [CustomAuthorize]
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
        [HttpPost("additional-details")]
        public async Task<IActionResult> FieldManagement(FieldManagementQuery queryParam, CancellationToken cancellationToken)
        {
            //Member Additional Details (FM)
            var dataList = new Dictionary<string, object>();
            var ItemIds = new List<int>();

            var formSchemaList = await _mediator.Send(new FieldManagementMemberFormQuery { Entity = "NGB", UserId = queryParam.UserId, ItemKey = "Class.Member_Overview_Form_for_class" });

            if (formSchemaList != null && formSchemaList.Count() > 0) ItemIds = formSchemaList.Select(m => m.TabId).ToList();

            if (ItemIds.Count() > 0)
            {
                dataList = await GetFMSchemaAndData(ItemIds, queryParam.MemberDocId);
            }
            return Ok(new ApiResponse<object, object>(dataList));
        }


        [CustomAuthorize]
        [HttpPost("note-list")]
        public async Task<IActionResult> GetNoteList(NoteListQuery queryParam, CancellationToken cancellationToken)
        {
            return Ok(new ApiResponse<object, object>(await _mediator.Send(queryParam, cancellationToken)));
        }


        [CustomAuthorize]
        [HttpPost("occurrence-note")]
        public async Task<IActionResult> GetOccurrenceNote(OccurrenceNoteQuery queryParam, CancellationToken cancellationToken)
        {
            return Ok(new ApiResponse<object, object>(await _mediator.Send(queryParam, cancellationToken)));
        }

        [CustomAuthorize]
        [HttpPut("note-update")]
        public async Task<IActionResult> UpdateNote(SingleNoteUpdateCommand queryParam, CancellationToken cancellationToken)
        {
            return Ok(new ApiResponse<object, object>(await _mediator.Send(queryParam, cancellationToken)));
        }

        [CustomAuthorize]
        [HttpDelete("note-delete")]
        public async Task<IActionResult> DeleteNote(SingleNoteDeleteCommand queryParam, CancellationToken cancellationToken)
        {
            return Ok(new ApiResponse<object, object>(await _mediator.Send(queryParam, cancellationToken)));
        }

        [CustomAuthorize]
        [HttpPost("eligibility")]
        public async Task<IActionResult> CheckEligibility(EligibilityQueryParam queryParam, CancellationToken cancellationToken)
        {

            //Member with emergency details
            var details = await _mediator.Send(new MemberDetailsQuery { MemberDocId = queryParam.MemberDocId });
            if (details == null || details.Count == 0) return Ok(new ApiResponse<object, object>(null,200,"No data found!"));
            var memberDetails = ConvertToModel(details);


            //Get member Eligibility
            var eligibilityData = await _mediator.Send(new SessionEligibilityRulesQuery { ProductId = queryParam.ProductId, UserId = memberDetails.PersonalInfo.UserId, MemberDocId = queryParam.MemberDocId });
            //if (eligibilityData == null) return Ok(new ApiResponse<object>(null)); 

            var paymentData = await _mediator.Send(new SessionPaymentRulesQuery { ProductId = queryParam.ProductId, AttendeeId = queryParam.AttendeeId, OccurrenceId = queryParam.OccurrenceId });

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


        #endregion old
    }
}
