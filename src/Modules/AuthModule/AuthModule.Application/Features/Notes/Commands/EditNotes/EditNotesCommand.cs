using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace AuthModule.Application.Features.Notes.Commands.EditNotes
{
    public class EditNotesCommand : IRequest<int>
    {
        public Guid NotesGuid { get; set; }
        public int EntityType { get; set; }
        public Guid EntityId { get; set; }
        public string Details { get; set; }
        public string Module { get; set; }
        [DefaultValue(false)]
        public bool? IsMailSend { get; set; }
    }
}
