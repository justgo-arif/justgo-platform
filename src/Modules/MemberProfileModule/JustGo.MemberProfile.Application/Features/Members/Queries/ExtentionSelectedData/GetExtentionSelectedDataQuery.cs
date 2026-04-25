using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.MemberProfile.Application.Features.Members.Queries.ExtentionSelectedData
{
    public class GetExtentionSelectedDataQuery : IRequest<Dictionary<string, object>>
    {
        public int ExId { get; }
        public int DocId { get; }

        public GetExtentionSelectedDataQuery(int exId, int docId)
        {
            ExId = exId;
            DocId = docId;
        }
    }
}
