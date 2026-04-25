using JustGo.Authentication.Services.Interfaces.CustomMediator;
using System;

namespace JustGo.AssetManagement.Application.Features.AssetLeases.Queries.IsExistsLeaseCartItem
{
    public class IsExistsLeaseCartItemQuery : IRequest<bool>
    {
        public Guid LeaseId { get; set; }
        public int EntityType { get; set; }
    }
}