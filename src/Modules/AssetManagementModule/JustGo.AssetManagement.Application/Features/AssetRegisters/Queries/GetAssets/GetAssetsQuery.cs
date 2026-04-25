using JustGo.AssetManagement.Domain.Entities;


namespace JustGo.AssetManagement.Application.Features.AssetRegisters.Queries.GetAssets
{
    public class GetAssetsQuery : GetAdminAssetsQuery
    {
        public bool SkipHierarchyAssetsMode { get; set; }
    }
}
  