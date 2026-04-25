using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace MobileApps.Application.Features.Class.V3.Queries
{
    public class OccurrenceNoteQuery : IRequest<IDictionary<string,object>> 
    {
        [Required]
        public int OccurrenceId { get; set; }
        [Required]
        public int AttendeeId { get; set; }
    }
}
