using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using MobileApps.Domain.Entities.V2.FieldManagement;

namespace MobileApps.Application.Features.FieldManagement.Queries.GetFieldManagementSchema
{
    public class FieldManagementSchemaQuery:IRequest<EntityExtensionSchema>
    {
        public required int ExId { get; set; }
        
    }
}
