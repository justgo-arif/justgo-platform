using JustGo.AssetManagement.Application.DTOs;
using JustGo.AssetManagement.Application.DTOs.Common;
using JustGo.Authentication.Helper.Paginations.Offset;
using JustGo.Authentication.Services.Interfaces.CustomMediator;


namespace JustGo.AssetManagement.Application.Features.AssetOwnershipTransfers.Queries.GetTransferActivityLog
{
    public class GetTransferActivityLogQuery : PaginationParams, IRequest<PagedResult<TransferActivityLogItemDTO>>
    {
        public string AssetTransferId { get; set; }
        public bool? IsDescending { get; set; }

    }
}
  