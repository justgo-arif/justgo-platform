using JustGo.Authentication.Services.Interfaces.CustomMediator;


namespace JustGo.AssetManagement.Application.Features.AssetRegisters.Queries.GetCurrentAssetStep
{
    public class GetCurrentAssetStepQuery : IRequest<string>
    {
        public string AssetRegisterId { get; set; } 
    }
}
