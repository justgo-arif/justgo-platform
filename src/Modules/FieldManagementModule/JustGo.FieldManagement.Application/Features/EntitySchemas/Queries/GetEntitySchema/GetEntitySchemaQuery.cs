using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JustGo.FieldManagement.Domain.Entities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.FieldManagement.Application.Features.EntitySchemas.Queries.GetEntitySchema
{
    public class GetEntitySchemaQuery : IRequest<EntityExtensionSchema>
    {
        public GetEntitySchemaQuery(string ownerType, int ownerId, string extensionArea, int extensionEntityId, bool isArena = false)
        {
            OwnerType = ownerType;
            OwnerId = ownerId;
            ExtensionArea = extensionArea;
            ExtensionEntityId = extensionEntityId;
            IsArena = isArena;
        }

        public string OwnerType { get; set; }
        public int OwnerId { get; set; }
        public string ExtensionArea { get; set; }
        public int ExtensionEntityId { get; set; }
        public bool IsArena { get; set; }
    }
}
