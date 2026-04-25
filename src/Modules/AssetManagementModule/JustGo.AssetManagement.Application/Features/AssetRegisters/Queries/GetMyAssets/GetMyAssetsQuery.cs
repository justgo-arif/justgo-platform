using JustGo.AssetManagement.Application.DTOs;
using JustGo.AssetManagement.Application.DTOs.Common;
using JustGo.AssetManagement.Domain.Entities;
using JustGo.Authentication.Helper.Paginations.Offset;
using JustGo.Authentication.Services.Interfaces.CustomMediator;


namespace JustGo.AssetManagement.Application.Features.AssetRegisters.Queries.GetMyAssets
{
    public class GetMyAssetsQuery : PaginationParams, IRequest<PagedResult<AssetListItemDTO>>
    {
        public List<SortItemDTO> SortItems { get; set; } = new List<SortItemDTO>();
        public List<SearchSegmentDTO> SearchItems { get; set; } = new List<SearchSegmentDTO>(); 

    }
}
  