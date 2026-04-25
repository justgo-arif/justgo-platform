using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace MobileApps.Application.Features.Class.V2.Queries.GetClassBookingList
{
    public class GetClassListQuery:IRequest<IList<IDictionary<string,object>>>
    {
        [Required]
        public string? ClubSyncGuid { get; set; } = "";
        public string ClassName { get; set; } = default!;
        public DateTime? StartDate { get; set; } 
        public DateTime? EndDate { get; set; }
        
    }
}
