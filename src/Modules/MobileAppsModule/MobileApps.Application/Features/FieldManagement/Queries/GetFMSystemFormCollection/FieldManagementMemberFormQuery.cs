using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using MobileApps.Domain.Entities.V2.FieldManagement;

namespace MobileApps.Application.Features.FieldManagement.Queries.GetFMSystemFormCollection 
{
    public class FieldManagementMemberFormQuery : IRequest<List<FormSchemaInfo>>
    {
        public required string Entity { get; set; }
        public required int UserId { get; set; }
        public required string ItemKey { get; set; }
    }
}
