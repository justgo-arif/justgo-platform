using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MobileApps.Domain.Entities.BaseEntity;

namespace MobileApps.Application.Features.Event.V2.Queries.GetAllBookingQuery
{
    public class BookingQuery:BasePagingClass
    {
        public int Id { get; set; } //for event 'EventDocId', recurring 'RowId'
        public string AttendeeName { get; set; } = default!;
        public List<int> TicketTypes { get; set; } = new List<int>();
        public List<string> AttendeeStatuses { get; set; } = new List<string>();
        public bool IsRecurring { get; set; } = false;
        public string? DateFilter { get; set; } = "";
       
    }
}
