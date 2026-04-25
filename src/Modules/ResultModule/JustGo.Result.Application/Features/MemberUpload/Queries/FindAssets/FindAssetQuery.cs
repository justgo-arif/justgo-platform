using JustGo.Result.Application.DTOs.ImportExportFileDtos;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.Result.Application.Features.MemberUpload.Queries.FindAssets
{
    public class FindAssetQuery : IRequest<List<FindAssetsDto>>
    {
        public string? SearchTerm { get; set; }
        public FindAssetQuery()
        {
        }
        public FindAssetQuery(string? searchTerm = null)
        {
            SearchTerm = searchTerm;
        }
    }
}