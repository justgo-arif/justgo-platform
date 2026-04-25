using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Result.Domain.Entities;

namespace JustGo.Result.Application.Features.MemberUpload.Queries.GetDisciplines
{
    public class GetDisciplinesQuery: IRequest<List<ResultDiscipline>>
    {
        public GetDisciplinesQuery(int scopeType, int sportTypeId,string? ownerGuid)
        {
            ScopeType = scopeType;
            SportTypeId = sportTypeId;
            OwnerGuid = ownerGuid;
        }

        public int ScopeType { get; set; }
        public int SportTypeId { get; set; }
        public string? OwnerGuid { get; set; }
    }
}


