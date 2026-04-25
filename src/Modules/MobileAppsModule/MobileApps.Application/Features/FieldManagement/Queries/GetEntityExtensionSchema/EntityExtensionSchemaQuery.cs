using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using MobileApps.Domain.Entities.V2.FieldManagement;

namespace MobileApps.Application.Features.FieldManagement.Queries.GetEntityExtensionSchema
{
    public class EntityExtensionSchemaQuery : IRequest<List<EntityExtensionSchemaCore>>
    {
        public required List<int> ItemIds { get; set; }    
    }
}
