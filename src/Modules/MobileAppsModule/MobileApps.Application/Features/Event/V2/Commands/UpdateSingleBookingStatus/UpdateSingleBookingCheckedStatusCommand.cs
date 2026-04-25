using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace MobileApps.Application.Features.Event.V2.Commands
{
    public class UpdateSingleBookingCheckedStatusCommand : IRequest<Dictionary<string, object>>
    {
        public required int CourseBookingDocId { get; set; }
        public int RowId { get; set; } = default!;
        public string AttendeeStatus { get; set; } = default!;
        public string Note { get; set; } = "";
        public bool IsRecurringEvent { get; set; } = false;
        public required DateTime CheckingDate { get; set; }
       
        public required DateTime CheckedInAt { get; set; }   
        public required int  TimeZoneId { get; set; }   
    }
    
}
