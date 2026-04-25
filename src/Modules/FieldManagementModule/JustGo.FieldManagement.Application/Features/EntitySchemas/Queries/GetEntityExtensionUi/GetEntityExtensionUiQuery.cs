using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.FieldManagement.Domain.Entities;

namespace JustGo.FieldManagement.Application.Features.EntitySchemas.Queries.GetEntityExtensionUi
{
    public class GetEntityExtensionUiQuery : IRequest<List<EntityExtensionUI>>
    {
        public GetEntityExtensionUiQuery(string ownerType, int ownerId, string extensionArea, int extensionEntityId)
        {
            OwnerType = ownerType;
            OwnerId = ownerId;
            ExtensionArea = extensionArea;
            ExtensionEntityId = extensionEntityId;
        }

        public string OwnerType { get; set; }
        public int OwnerId { get; set; }
        public string ExtensionArea { get; set; }
        public int ExtensionEntityId { get; set; }
    }
}
