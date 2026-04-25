using JustGo.AssetManagement.Application.DTOs;
using JustGo.AssetManagement.Application.DTOs.Common;
using JustGo.Authentication.Helper.Paginations.Offset;
using JustGo.Authentication.Services.Interfaces.CustomMediator;


namespace JustGo.AssetManagement.Application.Features.AssetLeases.Queries.GetLeaseHistory
{
    public class GetLeaseHistoryQuery : PaginationParams, IRequest<PagedResult<LeaseHistoryItemDTO>>
    {
        public string AssetRegisterId { get; set; }
        public List<SortItemDTO> SortItems { get; set; } = new List<SortItemDTO>();
        public List<SearchSegmentDTO> SearchItems { get; set; } = new List<SearchSegmentDTO>(); 

    }
}
  