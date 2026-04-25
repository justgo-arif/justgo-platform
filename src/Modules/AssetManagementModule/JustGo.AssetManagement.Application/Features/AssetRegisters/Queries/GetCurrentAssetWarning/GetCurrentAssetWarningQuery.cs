using JustGo.AssetManagement.Application.DTOs;
using JustGo.Authentication.Services.Interfaces.CustomMediator;


namespace JustGo.AssetManagement.Application.Features.AssetRegisters.Queries.GetCurrentAssetWarning
{
    public class GetCurrentAssetWarningQuery : IRequest<AssetNotificationModel>
    {
        public string AssetRegisterId { get; set; } 
    }
}
