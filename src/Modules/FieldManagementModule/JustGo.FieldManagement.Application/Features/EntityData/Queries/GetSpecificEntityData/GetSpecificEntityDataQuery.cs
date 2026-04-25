using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.FieldManagement.Application.Features.EntityData.Queries.GetSpecificEntityData
{
    public class GetSpecificEntityDataQuery : IRequest<Dictionary<string, object>>
    {
        public int ExId { get; }
        public string ItemId { get; }
        public int EntityId { get; }

        public GetSpecificEntityDataQuery(int exId, string itemId, int entityId)
        {
            ExId = exId;
            ItemId = itemId;
            EntityId = entityId;
        }
    }
}
