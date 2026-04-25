using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using MobileApps.Domain.Entities.V2.Event;

namespace MobileApps.Application.Features.Class.V2.Command.SingleAttendanceUpdate
{
    public class SingleAttendanceUpdateCommand : IRequest<bool>
    {
        public AttendanceUpdateCommandModel attendance { get; set; }
        public SingleAttendanceUpdateCommand(AttendanceUpdateCommandModel attendanceModel)
        {
            attendance = attendanceModel;
        }
    }

}
