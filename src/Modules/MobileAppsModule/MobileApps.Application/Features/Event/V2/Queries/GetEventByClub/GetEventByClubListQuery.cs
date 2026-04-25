using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace MobileApps.Application.Features.Event.V2.Queries.GetEventByClub  
{
    public class GetEventByClubListQuery : IRequest<IList<IDictionary<string, object>>>
    {
        [Required]
        public int ClubDocId { get; set; }  
        public GetEventByClubListQuery(int id)
        {
                this.ClubDocId = id;
        }
    }
}
