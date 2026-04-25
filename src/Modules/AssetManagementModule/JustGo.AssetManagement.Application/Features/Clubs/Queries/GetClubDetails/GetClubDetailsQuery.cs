using JustGo.AssetManagement.Application.DTOs;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.AssetManagement.Application.Features.Clubs.Queries.GetClubDetails
{
    public class GetClubDetailsQuery:IRequest<ClubMemberDTO>
    {
        public string ClubId { get; set; }
    }
}
