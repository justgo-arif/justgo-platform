using JustGo.AssetManagement.Application.DTOs.Enums;
using JustGo.Authentication.Services.Interfaces.CustomMediator;


namespace JustGo.AssetManagement.Application.Features.Common.Queries.GetIdByGuid
{
    public class GetIdByGuidQuery:IRequest<List<int>>
    {

        public List<string> RecordGuids { get; set; }
        public AssetTables Entity { get; set; }
    }
}
