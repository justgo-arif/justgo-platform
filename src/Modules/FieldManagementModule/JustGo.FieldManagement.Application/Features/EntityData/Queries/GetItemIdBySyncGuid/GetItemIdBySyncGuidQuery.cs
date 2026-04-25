using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.FieldManagement.Application.Features.EntityData.Queries.GetItemIdBySyncGuid
{
    public class GetItemIdBySyncGuidQuery : IRequest<string>
    {
        public string SyncGuid { get; }
        public GetItemIdBySyncGuidQuery(string syncGuid)
        {
            SyncGuid = syncGuid;
        }
    }
}
