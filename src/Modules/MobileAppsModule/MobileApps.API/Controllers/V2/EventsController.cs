using System.Threading;
using Asp.Versioning;
using JustGo.Authentication.Infrastructure.JwtAuthentication;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using MobileApps.Application.Features.Event.V2.Commands;
using MobileApps.Application.Features.Event.V2.Queries.AttendanceStatus;
using MobileApps.Application.Features.Event.V2.Queries.GetAllBookingQuery;
using MobileApps.Application.Features.Event.V2.Queries.GetBothEventList;
using MobileApps.Application.Features.Event.V2.Queries.GetEventBookingList;
using MobileApps.Application.Features.Event.V2.Queries.GetEventList;
using MobileApps.Application.Features.Event.V2.Queries.GetEventListPaging;
using MobileApps.Application.Features.Event.V2.Queries.GetEventOccurrenceBookingList;
using MobileApps.Application.Features.Event.V2.Queries.GetEventTicketTypeList;
using MobileApps.Application.Features.Event.V2.Queries.GetRecurringEventList;
using MobileApps.Application.Features.Event.V2.Queries.GetRecurringEventListPaging;
using MobileApps.Application.Features.Event.V2.Queries.GetRecurringEventOccuranceList;
using MobileApps.Application.Features.Event.V2.Queries.GetRecurringEventTicketTypeList;
using MobileApps.Application.Features.Event.V2.Queries.GetRecurringOccuranceBookingDateList;
using MobileApps.Application.Features.Event.V2.Queries.ValidateBooking;
using MobileApps.Application.Features.Event.V2.Queries.ValidateEventBooking;
using MobileApps.Application.Features.Event.V2.Queries.ValidateRecurringBooking;
using MobileApps.Domain.Entities.V2.Event;
using Newtonsoft.Json;


namespace MobileApps.API.Controllers.V2
{
    [ApiVersion("2.0")]
    [ApiVersion("3.0")]
    [Route("api/v{version:apiVersion}/events")]
    [ApiController]
    [Tags("Mobile Apps/Events")]
    public class EventsController : ControllerBase
    {
        IMediator _mediator;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public EventsController(IMediator mediator, IHttpContextAccessor httpContextAccessor)
        {
            _mediator = mediator;
            _httpContextAccessor = httpContextAccessor;
        }


        [CustomAuthorize]
        [HttpPost("list")]
        public async Task<IActionResult> GetEventsAsync(GetEventListQuery query)
        {
            var combinedList = new List<IDictionary<string, object>>();
           

            var eventData = await _mediator.Send(query);
            if(eventData.Count>0) combinedList.AddRange(eventData);

            var recurringData = await _mediator.Send(new GetRecurringEventListQuery { ClubDocId = query.ClubDocId, EventName = query.EventName, StartDate = query.StartDate, EndDate = query.EndDate });
            if (recurringData.Count > 0) combinedList.AddRange(recurringData);

            var message = combinedList.Count() == 0 ? "No data found!" : "Data retrieved successfully";


            return Ok(new ApiResponse<object, object>(combinedList,200,message));
        }
        [CustomAuthorize]
        [HttpPost("all")]
        public async Task<IActionResult> GetEventsWithPagingAsync(GetAllEventListQuery query,CancellationToken cancellationToken)
        {
            int totalCount = 0;
            int nextId = 0;

            var eventData = await _mediator.Send(query, cancellationToken);
            if (eventData.Count > 0)
            {

                totalCount = Convert.ToInt32(eventData.FirstOrDefault()["TotalCount"]);
                nextId = Convert.ToInt32(eventData.LastOrDefault()["RowId"]);
            }



            var message = eventData.Count() == 0 ? "No data found!" : "Data retrieved successfully";
            if (eventData.Any())
            {

                eventData = eventData.OrderByDescending(dict => dict["IsTodayEvent"]).ToList();
                //// Keys to remove
                var keysToRemove = new[] { "TotalCount", "RowId" };
                // Fast iteration over the list
                for (int i = 0; i < eventData.Count(); i++)
                {
                    // Remove each key if it exists
                    for (int j = 0; j < keysToRemove.Length; j++)
                    {
                        eventData[i].Remove(keysToRemove[j]);
                    }
                }
            }
            return Ok(new ApiResponseWithCount<object, object>(eventData, null, totalCount, nextId, 200));
        }
        [CustomAuthorize]
        [HttpPost("booking-list")]
        public async Task<IActionResult> GetEventBookingListAsync(BookingQuery bookingQuery)
        {
          
            var combinedList = new List<IDictionary<string, object>>();
            int totalCount = 0;
            int nextId = 0;
            //sort sql query order
            bookingQuery.SortOrder= bookingQuery.SortOrder.ToLower()== "asc" ? "ASC" : "DESC";
          

            //getting regular event data
            if (!bookingQuery.IsRecurring)
            {
                var eventBookedList = await _mediator.Send(new GetEventBookingListQuery { EventDocId = bookingQuery.Id, AttendeeName = bookingQuery.AttendeeName, TicketTypes = bookingQuery.TicketTypes, AttendeeStatuses = bookingQuery.AttendeeStatuses,NextId=bookingQuery.NextId,DataSize= bookingQuery.DataSize,SortOrder= bookingQuery.SortOrder});

                if (eventBookedList.Count > 0) combinedList.AddRange(eventBookedList);

                eventBookedList.Clear();
            }
            //getting recurring event data
            else if (bookingQuery.IsRecurring)
            {
                var recurringBookedList = await _mediator.Send(new GetEventOccurrenceBookingListQuery { OccuranceRowId = bookingQuery.Id, AttendeeName = bookingQuery.AttendeeName, TicketTypes = bookingQuery.TicketTypes, AttendeeStatuses = bookingQuery.AttendeeStatuses, OccuranceDate =bookingQuery.DateFilter,NextId= bookingQuery.NextId,DataSize= bookingQuery.DataSize, SortOrder = bookingQuery.SortOrder });

                if (recurringBookedList.Count > 0) combinedList.AddRange(recurringBookedList);

                recurringBookedList.Clear();
            }

            var message = combinedList.Count() == 0 ? "No data found!" : "Data retrived successfully";

            if(combinedList.Count > 0)
            {
                totalCount = Convert.ToInt32(combinedList.LastOrDefault()["TotalCount"]) ;
                nextId =Convert.ToInt32(combinedList.LastOrDefault()["RowNumberId"]);
                
                // Keys to remove
                var keysToRemove = new[] { "TotalCount", "RowNumberId" };

                // Fast iteration over the list
                for (int i = 0; i < combinedList.Count; i++)
                {
                    var dict = combinedList[i];

                    // Remove each key if it exists
                    for (int j = 0; j < keysToRemove.Length; j++)
                    {
                        dict.Remove(keysToRemove[j]);
                    }
                }
            }
        
            return Ok(new ApiResponseWithCount<object, object>(combinedList,null, totalCount, nextId,200,message));
        }

        [CustomAuthorize]
        [HttpPost("occurrence-booking-date/{id}")]
        public async Task<IActionResult> GetEventOccurenceBookingDateAsync(int id)
        {
            //getting recurring event occurrence booking date
            return Ok(new ApiResponse<object, object>(await _mediator.Send(new GetRecurringOccuranceBookingDateListQuery { RowId = id })));
        }



        [CustomAuthorize]
        [HttpPost("validate-booking-qr")]
        public async Task<IActionResult> ValidateBookingQRAsync(ValidateBookingQuery validateQuery)   
        {
         
            var result = await _mediator.Send(validateQuery);
            if (result.Count == 0 || Convert.ToInt32( result["EventDocId"])!= validateQuery.DocId) { 
                return Ok(new ApiResponse<object, object>(null,200, "Your Ticket isn't valid for this event", "error"));
            }
            if ((bool)result["IsRecurring"])
            {
               var recurringData = await _mediator.Send(new ValidateRecurringBookingQRQuery { DocId = Convert.ToInt32(result["DocId"]) });

                recurringData.Item1.Add("DocId", Convert.ToInt32(result["DocId"]));

                var expectedData =JsonConvert.DeserializeObject<List<BookingDate>>(JsonConvert.SerializeObject(recurringData.Item1["BookingDateList"]));

                var filterData = expectedData?.Where(m => Convert.ToDateTime(m.ScheduleDate).Date == validateQuery.BookingDate.Date).FirstOrDefault();

                if (filterData!=null)
                {
                    var memberDetail=await _mediator.Send(new SetAttendenceForOccuranceBookingCommand { CourseBookingDocId = Convert.ToInt32(result["DocId"]), RowId = filterData.RowId, AttendeeStatus = "Checked In", CheckedInAt = validateQuery.CheckedInAt, CheckingDate = validateQuery.BookingDate });

                    return Ok(new ApiResponse<object, object>(new Tuple<IDictionary<string, object>, bool>(memberDetail, recurringData.Item2)));
                  
                }
                else
                {
                    return Ok(new ApiResponse<object, object>(null, 200, "Date selection is wrong", "error"));
                }

               
            }
            else
            {
                return Ok(new ApiResponse<object, object>(await _mediator.Send(new ValidateEventBookingQuery { DocId = Convert.ToInt32(result["DocId"]), CheckedInAt = validateQuery.CheckedInAt, BookingDate = validateQuery.BookingDate })));
            }
            
        }

        [CustomAuthorize]
        [HttpPost("update-booking-list")]
        public async Task<IActionResult> UpdateBookingListStatus(UpdateBookingCheckedStatusCommand command)
        {
            return Ok(new ApiResponse<object, object>(await _mediator.Send(command)));
        }

        [CustomAuthorize]
        [HttpPost("update-single-booking")]
        public async Task<IActionResult> UpdateSingleBookingStatus(UpdateSingleBookingCheckedStatusCommand command,CancellationToken cancellationToken)
        {
            var retult = await _mediator.Send(command, cancellationToken);
            if (retult.Count > 0)
            {
                string message = Convert.ToBoolean(retult["IsExecute"]) ? "Request successful": "Failed to update attendee. Please try again.";
                return Ok(new ApiResponse<object, object>(retult, 200, message));
            }
            return Ok(new ApiResponse<object, object>(await _mediator.Send(command, cancellationToken)));
        }

 
        [CustomAuthorize]
        [HttpPost("ticket-list")]
        public async Task<IActionResult> GetEventTicketTypeList(TicketTypeQueryModel queryModel)
        {
            if (!queryModel.IsRecurring)
                return Ok(new ApiResponse<object, object>(await _mediator.Send(new GetEventTicketTypeListQuery(queryModel.Id))));
            else
                return Ok(new ApiResponse<object, object>(await _mediator.Send(new GetRecurringEventTicketTypeListQuery(queryModel.Id))));
        }

        [CustomAuthorize]
        [HttpGet("attendance-status-list")]
        public async Task<IActionResult> GetAttendanceStatusList()
        {
            return Ok(new ApiResponse<object, object>(await _mediator.Send(new GetAttendanceStatusListQuery())));
           
        }


        [CustomAuthorize]
        [HttpPost("occurrence-list")]
        public async Task<IActionResult> GetOccurenceList(GetRecurringEventOccuranceListQuery queryParam)
        {
            return Ok(new ApiResponse<object, object>(await _mediator.Send(queryParam)));
        }


        [CustomAuthorize]
        [HttpGet("event-booking-count")]
        public async Task<IActionResult> GetEventBookingCount(long eventDocId)
        {
            return Ok(new ApiResponse<object, object>(await _mediator.Send(new GetEventBookingCountQuery { EventDocId = eventDocId })));
        }


        [CustomAuthorize]
        [HttpGet("recurring-booking-count")]
        public async Task<IActionResult> GetRecurringEventOccurenceBookingCount(int rowId,string dateFilter) 
        {
            return Ok(new ApiResponse<object, object>(await _mediator.Send(new GetEventOccuranceBookingCountQuery { RowId = rowId, OccuranceDate = dateFilter })));
          
        }
        [CustomAuthorize]
        [HttpPost("set-occurrence-attendance")]
        public async Task<IActionResult> SetAttendenceForOccurance(SetAttendenceForOccuranceBookingCommand command)
        {
            if (command.CheckedInAt == null) command.CheckedInAt = DateTime.UtcNow;
            return Ok(new ApiResponse<object, object>(await _mediator.Send(command)));

        }

    }
}
