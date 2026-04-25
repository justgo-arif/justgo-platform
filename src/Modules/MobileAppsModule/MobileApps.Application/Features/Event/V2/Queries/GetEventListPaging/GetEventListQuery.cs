using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using MobileApps.Domain.Entities.BaseEntity;

namespace MobileApps.Application.Features.Event.V2.Queries.GetEventListPaging   
{
    public class GetEventListPagingQuery:IRequest<IList<IDictionary<string,object>>>, IBasePaging
    {
        [Required]
        public int ClubDocId { get; set; }  
        public string EventName { get; set; } = default!;
        public string StartDate { get; set; } = "";
        public string? EndDate { get; set; } = "";
        [Required]
        public int NextId { get; set; } = 0;
        [Required]
        public int DataSize { get; set; } = 20;
        public string? SortOrder { get; set; }
    }
}
