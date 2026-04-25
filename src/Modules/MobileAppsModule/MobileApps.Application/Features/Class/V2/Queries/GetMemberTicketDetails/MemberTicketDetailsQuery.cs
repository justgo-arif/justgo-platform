using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using MobileApps.Domain.Entities.V2.FieldManagement;

namespace MobileApps.Application.Features.FieldManagement.Queries.GetFMSystemFormCollection 
{
    public class MemberTicketDetailsQuery : IRequest<IList<IDictionary<string,object>>>
    {
        public required int SessionId { get; set; }
        public required int UserId { get; set; }
        public required int MemberId { get; set; }
    }
}
