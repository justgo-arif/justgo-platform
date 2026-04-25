using JustGo.AssetManagement.Application.DTOs;
using JustGo.AssetManagement.Application.DTOs.Common;
using JustGo.Authentication.Helper.Paginations.Offset;
using JustGo.Authentication.Services.Interfaces.CustomMediator;


namespace JustGo.AssetManagement.Application.Features.AssetLeases.Queries.GetLeaseActivityLog
{
    public class GetLeaseActivityLogQuery : PaginationParams, IRequest<PagedResult<LeaseActivityLogItemDTO>>
    {
        public string AssetLeaseId { get; set; }
        public bool? IsDescending { get; set; }

    }
}
  