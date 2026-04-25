using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.AssetManagement.Application.Features.MasterLicenses.Commands.RemoveAssetCartItems
{
    public class RemoveAssetCartItemsCommand : IRequest<bool>
    {
        public RemoveAssetCartItemsCommand(Guid assetRegisterId, Guid productId)
        {
            AssetRegisterId = assetRegisterId;
            ProductId = productId;
        }

        public Guid AssetRegisterId { get; set; }
        public Guid ProductId { get; set; }
    }
}
