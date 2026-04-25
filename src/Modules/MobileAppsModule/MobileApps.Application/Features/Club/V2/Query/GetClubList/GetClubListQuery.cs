using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using MobileApps.Domain.Entities.V2;

namespace MobileApps.Application.Features.Club.V2.Query.GetClubList
{
    public class GetClubListQuery:IRequest<List<SwitcherClub>>
    {
        public int  UserId { get; set; }    
        public bool IsClubPlusOnly { get; set; } =true;    
        public bool IsStripeMode { get; set; } =false;    
    }   
}
