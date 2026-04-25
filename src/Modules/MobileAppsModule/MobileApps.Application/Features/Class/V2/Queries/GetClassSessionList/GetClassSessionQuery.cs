using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace MobileApps.Application.Features.Class.V2.Queries.GetClassSessionList
{
    public class GetClassSessionQuery:IRequest<IList<IDictionary<string,object>>>
    {
        [Required]
        public int ClassId { get; set; }
        public string SessionName { get; set; } = default!;
        public string? SessionStartDate { get; set; }
        public string? SessionEndDate { get; set; }
    }
}
