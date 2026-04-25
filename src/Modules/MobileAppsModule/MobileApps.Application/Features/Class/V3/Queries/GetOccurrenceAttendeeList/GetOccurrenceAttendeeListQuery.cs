using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using MobileApps.Domain.Entities.BaseEntity;
using MobileApps.Domain.Entities.V2.Classes;

namespace MobileApps.Application.Features.Class.V3.Queries
{
    public class GetOccurrenceAttendeeListQuery:IRequest<List<AttendeeDto>>, IBasePaging
    {
        public required List<int> OccurrenceIds { get; set; }
        public string? AttendeeName { get; set; }
        public List<int> TicketTypes { get; set; } = new List<int>();
        public List<string> AttendeeStatuses { get; set; } = new List<string>();
        public required int NextId { get; set; }
        public required int DataSize { get; set; }
        public string? SortOrder { get; set; } = "ASC";
        public int? ClubId { get; set; } = null;
    }
}
