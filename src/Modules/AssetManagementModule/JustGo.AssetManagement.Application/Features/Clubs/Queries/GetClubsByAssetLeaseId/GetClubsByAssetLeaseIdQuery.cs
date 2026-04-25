using JustGo.AssetManagement.Application.DTOs;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.AssetManagement.Application.Features.Clubs.Queries.GetClubsByAssetLeaseId
{
    public class GetClubsByAssetLeaseIdQuery : IRequest<List<ClubMemberDTO>>
    {

        public GetClubsByAssetLeaseIdQuery(string assetLeaseId)
        {
            AssetLeaseId = assetLeaseId;
        }
        public string AssetLeaseId { get; set; }
    }
}
