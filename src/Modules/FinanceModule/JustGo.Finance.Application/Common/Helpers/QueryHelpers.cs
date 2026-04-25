using Dapper;

namespace JustGo.Finance.Application.Common.Helpers
{
    public class QueryHelpers
    {
        public static DynamicParameters GetGuidParams(Guid syncGuid)
        {
            var p = new DynamicParameters();
            p.Add("SyncGuid", syncGuid);
            return p;
        }
        public static DynamicParameters GetPaymentDocIdParams(int docId)
        {
            var p = new DynamicParameters();
            p.Add("DocId", docId);
            return p;
        }
    }
}
