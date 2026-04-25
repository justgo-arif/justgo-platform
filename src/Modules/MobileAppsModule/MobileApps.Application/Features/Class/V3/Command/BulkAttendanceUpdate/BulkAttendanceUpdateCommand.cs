using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using MobileApps.Domain.Entities.V2.Event;

namespace MobileApps.Application.Features.Class.V3.Command.BulkAttendanceUpdate
{
    public class BulkAttendanceUpdateCommand:IRequest<bool>
    {
        public List<AttendanceUpdateCommandModel> attendanceList { get; set; }

        public BulkAttendanceUpdateCommand(List<AttendanceUpdateCommandModel> attendances)
        {
            attendanceList = attendances;
        }
    }
}
