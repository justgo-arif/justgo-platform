using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using MobileApps.Domain.Entities.V2;
using MobileApps.Domain.Entities.V2.Classes;

namespace MobileApps.Application.Features.Class.V3.Command.ClassNoteDelete
{
    public class SingleNoteDeleteCommand : IRequest<bool>
    {
        public int NoteId { get; set; }
    }

    
}
