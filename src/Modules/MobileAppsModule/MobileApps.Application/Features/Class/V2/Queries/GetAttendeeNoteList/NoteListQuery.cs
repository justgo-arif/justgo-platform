using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace MobileApps.Application.Features.Class.V2.Queries.GetAttendeeNoteList  
{
    public class NoteListQuery : IRequest<IList<IDictionary<string,object>>>
    {
        [Required]
        public int AttendeeId { get; set; }     
   
    }
}
