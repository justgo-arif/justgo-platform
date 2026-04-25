using JustGo.AssetManagement.Application.DTOs.Enums;
using JustGo.Authentication.Services.Interfaces.CustomMediator;


namespace JustGo.AssetManagement.Application.Features.Common.Queries.CheckLeasePedingByAssetId
{
    public class CheckLeasePedingByAssetIdQuery : IRequest<bool>
    {

        public string AssetRegisterId { get; set; }
    }
}
