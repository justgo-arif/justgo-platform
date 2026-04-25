using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.FieldManagement.Application.Features.EntityData.Commands.CreateSpecificEntityData
{
    public class CreateSpecificEntityDataCommand : IRequest<int>
    {
        public int ExId { get; set; }
        public string ItemId { get; }
        public int EntityId { get; set; }
        public Dictionary<string, object> Data { get; }

        public CreateSpecificEntityDataCommand(int exId,string itemId, int entityId, Dictionary<string, object> data)
        {
            ExId = exId;
            ItemId = itemId;
            EntityId = entityId;
            Data = data;
        }
    }
}
