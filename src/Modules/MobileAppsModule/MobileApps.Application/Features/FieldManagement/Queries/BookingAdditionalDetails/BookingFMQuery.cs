using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using MobileApps.Domain.Entities.V2.FieldManagement;

namespace MobileApps.Application.Features.FieldManagement.Queries.GetFMSystemFormCollection 
{
    public class BookingFMQuery : IRequest<List<BookingSchemaInfo>>
    {
        public required int SessionId { get; set; }
    }
}
