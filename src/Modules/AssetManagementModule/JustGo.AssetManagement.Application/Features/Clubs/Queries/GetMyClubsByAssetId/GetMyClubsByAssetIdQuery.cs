using JustGo.AssetManagement.Application.DTOs;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.AssetManagement.Application.Features.Clubs.Queries.GetMyClubsByAssetId
{
    public class GetMyClubsByAssetIdQuery:IRequest<List<ClubMemberDTO>>
    {

        public GetMyClubsByAssetIdQuery(Guid assetRegisterId)
        {
            AssetRegisterId = assetRegisterId;
        }
        public Guid AssetRegisterId { get; set; }
    }
}
