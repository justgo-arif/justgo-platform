using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using MobileApps.Domain.Entities.V2.FieldManagement;

namespace MobileApps.Application.Features.FieldManagement.Queries.GetFieldManagementSchemaId
{
    public class FieldManagementSchemaIdQuery:IRequest<EntityExtensionSchemaCore>
    {
        public required string OwnerType { get; set; }
        public required int OwnerId { get; set; }
        public required string ExtensionArea { get; set; }
        public int ExtensionEntityId { get; set; } = 0;
    }
}
