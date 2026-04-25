using JustGo.AssetManagement.Application.DTOs;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.AssetManagement.Application.Features.Clubs.Queries.GetMyClubs
{
    public class GetMyClubsQuery:IRequest<List<ClubMemberDTO>>
    {

        public GetMyClubsQuery(Guid memberId)
        {
            MemberId = memberId;
        }
        public Guid MemberId { get; set; }
    }
}
