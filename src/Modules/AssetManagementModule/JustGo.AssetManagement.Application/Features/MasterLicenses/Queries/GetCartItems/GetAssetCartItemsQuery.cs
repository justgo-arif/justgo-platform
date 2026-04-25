using JustGo.AssetManagement.Domain.Entities.Enums;
using JustGo.AssetManagement.Domain.Entities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.AssetManagement.Application.Features.MasterLicenses.Queries.GetCartItems
{
    public class GetAssetCartItemsQuery : IRequest<bool>
    {
        public GetAssetCartItemsQuery(Guid assetRegisterId, Guid productId)
        {
            AssetRegisterId = assetRegisterId;
            ProductId = productId;
        }
        public Guid AssetRegisterId { get; set; }
        public Guid ProductId { get; set; }
    }
}
