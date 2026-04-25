using JustGo.AssetManagement.Domain.Entities;
using JustGo.AssetManagement.Domain.Entities.Enums;
using JustGo.Authentication.Services.Interfaces.CustomMediator;


namespace JustGo.AssetManagement.Application.Features.Common.Queries.GetLeaseStatuses
{
    public class GetLeaseStatusIdQuery : IRequest<int>
    {
        public LeaseStatusType Status { get; set; }
    }
}
