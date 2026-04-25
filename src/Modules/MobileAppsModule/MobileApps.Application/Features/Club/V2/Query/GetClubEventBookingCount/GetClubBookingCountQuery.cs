using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace MobileApps.Application.Features.Club.V2.Query.GetClubEventBookingCount
{
    public class GetClubBookingCountQuery:IRequest<IList<IDictionary<string,object>>>
    {
        public long ClubDocId { get; set; }
        public DateTime? BookingDate { get; set; } = DateTime.UtcNow;
    }
}
