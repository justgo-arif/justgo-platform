using JustGo.AssetManagement.Application.DTOs.Enums;
using JustGo.Authentication.Services.Interfaces.CustomMediator;


namespace JustGo.AssetManagement.Application.Features.Common.Queries.CheckTranferPedingByAssetId
{
    public class CheckTranferPedingByAssetIdQuery : IRequest<bool>
    {

        public string AssetRegisterId { get; set; }
    }
}
