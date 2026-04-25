using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JustGo.FieldManagement.Domain.Entities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.FieldManagement.Application.Features.EntitySchemas.Commands.CreateEntitySchema
{
    public class CreateEntitySchemaCommand : EntityExtensionSchema,IRequest<EntityExtensionSchema>
    {
        public int? tabItemId { get; set; } = -1;
    }
}
