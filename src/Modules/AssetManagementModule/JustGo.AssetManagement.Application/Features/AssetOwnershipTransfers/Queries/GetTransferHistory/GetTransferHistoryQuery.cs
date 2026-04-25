using JustGo.AssetManagement.Application.DTOs;
using JustGo.AssetManagement.Application.DTOs.Common;
using JustGo.Authentication.Helper.Paginations.Offset;
using JustGo.Authentication.Services.Interfaces.CustomMediator;


namespace JustGo.AssetManagement.Application.Features.AssetOwnershipTransfers.Queries.GetTransferHistory
{
    public class GetTransferHistoryQuery : PaginationParams, IRequest<PagedResult<TransferHistoryItemDTO>>
    {
        public string AssetRegisterId { get; set; }
        public List<SortItemDTO> SortItems { get; set; } = new List<SortItemDTO>();
        public List<SearchSegmentDTO> SearchItems { get; set; } = new List<SearchSegmentDTO>(); 

    }
}
  