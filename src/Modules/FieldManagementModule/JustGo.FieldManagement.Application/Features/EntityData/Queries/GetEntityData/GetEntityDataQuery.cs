using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.FieldManagement.Application.Features.EntityData.Queries.GetEntityData
{
    public class GetEntityDataQuery : IRequest<Dictionary<string, object>>
    {
        public int ExId { get; }
        public int DocId { get; }

        public GetEntityDataQuery(int exId, int docId)
        {
            ExId = exId;
            DocId = docId;
        }
    }
}
