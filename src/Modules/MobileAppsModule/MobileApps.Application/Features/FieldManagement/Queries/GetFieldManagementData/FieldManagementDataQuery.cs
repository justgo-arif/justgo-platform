using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using MobileApps.Domain.Entities.V2.FieldManagement;

namespace MobileApps.Application.Features.FieldManagement.Queries.GetFieldManagementSchema
{
    public class FieldManagementDataQuery : IRequest<List<object>>
    {
        public required int MemberDocId { get; set; }
        public required EntityExtensionSchemaCore SchemaCore { get; set; }
    }
}
