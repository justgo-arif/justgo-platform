using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using Microsoft.AspNetCore.Mvc.Filters;
using MobileApps.Domain.Entities.BaseEntity;

namespace MobileApps.Application.Features.Event.V2.Queries.GetBothEventList
{
    public class GetAllEventListQuery : IRequest<IList<IDictionary<string,object>>>, IBasePaging
    {
        [Required]
        public int ClubDocId { get; set; }  
        public string EventName { get; set; } = default!;
        public string StartDate { get; set; } = "";
        public string? EndDate { get; set; } = "";
        public bool IsETicket { get; set; } = false;
        public int NextId { get; set; } = 0;
        public int DataSize { get; set; } = 50;
        public string SortOrder { get; set; } = "ASC";
    }
}
