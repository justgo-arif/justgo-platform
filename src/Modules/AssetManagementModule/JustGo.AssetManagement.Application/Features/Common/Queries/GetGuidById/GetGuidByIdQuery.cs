using JustGo.AssetManagement.Application.DTOs.Enums;
using JustGo.Authentication.Services.Interfaces.CustomMediator;


namespace JustGo.AssetManagement.Application.Features.Common.Queries.GetGuidById
{
    public class GetGuidByIdQuery : IRequest<List<string>>
    {

        public List<int> Ids { get; set; }
        public AssetTables Entity { get; set; }
    }
}
