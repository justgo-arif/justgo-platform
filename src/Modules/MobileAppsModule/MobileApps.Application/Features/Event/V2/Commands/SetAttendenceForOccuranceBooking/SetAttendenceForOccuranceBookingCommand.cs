using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace MobileApps.Application.Features.Event.V2.Commands
{
    public class SetAttendenceForOccuranceBookingCommand : IRequest<IDictionary<string,object>>
    {
        //This is used to set the attendance for a specific occurrence of a booking in a recurring event (QR Checking).
        public required int CourseBookingDocId { get; set; }
        public int RowId { get; set; }
        public required string AttendeeStatus { get; set; }
        public string Note { get; set; } = "";
        public required DateTime CheckingDate { get; set; }
        public DateTime? CheckedInAt { get; set; } = DateTime.UtcNow;
    }
    
}
