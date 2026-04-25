using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using Microsoft.AspNetCore.Mvc.Filters;
using MobileApps.Domain.Entities.BaseEntity;
using MobileApps.Domain.Entities.V4;

namespace MobileApps.Application.Features.Class.V3.Queries 
{ 
    public class GetClassListQuery:IRequest<IList<IDictionary<string, object>>>, IBasePaging, IClassPropertyExtend
    {
        [Required]
        public required string ClubGuid { get; set; }
        public string? ClassName { get; set; } = default!;
        public DateTime? StartDate { get; set; } 
        public string? TimeFilter { get; set; }
        public List<int>? AgeGroupIds { get; set; }
        public List<int>? ColorGroupIds { get; set; }
        public List<int>? CategoryIds { get; set; }
        public string[]? Genders { get; set; }
        public List<int>? CoachIds { get; set; }
        public List<int>? ProductTypeIds { get; set; } 
        public required int  NextId { get; set ; }
        public required int DataSize { get; set; }
        public string? SortOrder { get; set; }
        public DateTime? EndDate { get; set; }
    }
}
