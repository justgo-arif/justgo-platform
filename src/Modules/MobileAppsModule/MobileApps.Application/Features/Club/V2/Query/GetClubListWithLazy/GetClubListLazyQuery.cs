using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using Microsoft.AspNetCore.Mvc.Filters;
using MobileApps.Domain.Entities.BaseEntity;
using MobileApps.Domain.Entities.V2;

namespace MobileApps.Application.Features.Club.V2.Query.GetClubListWithLazy
{
    public class GetClubListLazyQuery:IRequest<List<Dictionary<string, object>>>,IBasePaging
    {
        public int  UserId { get; set; }    
        public bool IsClubPlusOnly { get; set; } =true;    
        public bool IsStripeMode { get; set; } =false;
        public string? ClubName { get; set; } = "";
        [Required]
        public int NextId { get; set; }
        [Required]
        public int DataSize { get; set; }
        public string? SortOrder { get; set; }
    }   
}
