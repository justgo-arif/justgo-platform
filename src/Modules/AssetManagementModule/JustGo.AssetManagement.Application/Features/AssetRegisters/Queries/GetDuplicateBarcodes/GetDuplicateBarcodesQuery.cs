using JustGo.AssetManagement.Application.DTOs;
using JustGo.Authentication.Services.Interfaces.CustomMediator;


namespace JustGo.AssetManagement.Application.Features.AssetRegisters.Queries.GetSingleAsset
{
    public class GetDuplicateBarcodesQuery : IRequest<bool>
    {
        public string AssetRegisterId { get; set; }
        public string Barcode { get; set; }

    }
}
  