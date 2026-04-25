using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Booking.Application.DTOs.BookingClassesDTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.Booking.Application.Features.BookingClasses.Queries.AttendeeListByOccurence
{
    public class GetAttendeeListByOccurenceQuery(
        Guid sessionGuid,
        Guid ownerGuid,
        int occurrenceId,
        int rowsPerPage,
        int pageNumber,
        bool isActiveMemberOnly,
        string? filterValue
        )
        : IRequest<SessionAttendanceResponseDto>
    {
        public Guid SessionGuid { get; } = sessionGuid;
        public Guid OwnerGuid { get; } = ownerGuid;
        public int OccurrenceId { get; } = occurrenceId;
        public int RowsPerPage { get; } = rowsPerPage;
        public int PageNumber { get; } = pageNumber;
        public bool IsActiveMemberOnly { get; } = isActiveMemberOnly;
        public string? FilterValue { get; } = filterValue;
    }
}



