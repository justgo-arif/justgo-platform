using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using MobileApps.Domain.Entities.V2.FieldManagement;

namespace MobileApps.Application.Features.FieldManagement.Queries.GetFieldManagementSchema
{
    public class FieldManagementFormatedSchemaQuery : IRequest<List<object>>
    {
        public required int ExId { get; set; }
        public required int ItemId { get; set; }
        public required string EntityType { get; set; } = "Ngb";//club;
        
    }
}
