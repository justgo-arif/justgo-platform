using JustGo.AssetManagement.Domain.Entities.Enums;
using JustGo.Authentication.Services.Interfaces.CustomMediator;


namespace JustGo.AssetManagement.Application.Features.Common.Queries.GetTransferStatuses
{
    public class GetTransferStatusIdQuery : IRequest<int>
    {
        public TransferStatusType Status { get; set; }
    }
}
