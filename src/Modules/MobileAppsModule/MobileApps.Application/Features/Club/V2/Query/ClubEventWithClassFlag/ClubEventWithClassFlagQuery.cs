using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using MobileApps.Domain.Entities;
using MobileApps.Domain.Entities.V2.Clubs;

namespace MobileApps.Application.Features.Club.Queries.GetClubList.V2
{
    public class ClubEventWithClassFlagQuery : IRequest<List<ClubEventWithClassFlagResponseDto>>
    {
        public List<ClubEventWithClassFlagDto> ClubIds { get; set; }
    }   
}
