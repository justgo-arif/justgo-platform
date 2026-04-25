using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace MobileApps.Application.Features.Event.V2.Queries.GetEventList
{
    public class GetEventListQuery:IRequest<IList<IDictionary<string,object>>>
    {
        [Required]
        public int ClubDocId { get; set; }  
        public string EventName { get; set; } = default!;
        public string StartDate { get; set; } = "";
        public string? EndDate { get; set; } = "";

    }
}
