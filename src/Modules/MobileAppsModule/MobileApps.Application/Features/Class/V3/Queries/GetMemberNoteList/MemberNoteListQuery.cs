using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace MobileApps.Application.Features.Class.V3.Queries
{
    public class MemberNoteListQuery : IRequest<IList<IDictionary<string,object>>>
    {
        public required Guid UserGuid { get; set; }
        public int? TimeZoneId { get; set; }

    }
}
