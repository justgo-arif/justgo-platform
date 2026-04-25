using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace MobileApps.Application.Features.Event.V2.Commands
{
    public class UpdateBookingCheckedStatusCommand : IRequest<bool>
    {
        public List<UpdateBookingStatus> UpdateBookingStatuses { get; set; }
        public bool IsRecurringEvent { get; set; } = false;

        public UpdateBookingCheckedStatusCommand(List<UpdateBookingStatus> bookingStatuses)
        {
           UpdateBookingStatuses = bookingStatuses;
        }
    }
    public class UpdateBookingStatus
    {
        [Required]
        public int CourseBookingDocId { get; set; }
        public int RowId { get; set; } = 0;
        public string Note { get; set; } = "";
        [Required]
        public string AttendeeStatus { get; set; }
        [Required]
        public DateTime AttendanceDate { get; set; }
        [Required]
        public DateTime CheckedInAt { get; set; } = DateTime.UtcNow;

    }

}
