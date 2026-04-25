using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.MemberProfile.Application.Features.MemberNotes.Commands.ChangeStatusOfNotes
{
  
    public class ChangeStatusOfNotesCommand() : IRequest<Result<string>>
    {
        public  required string MemberNoteGuid { get; set; }
        public bool? IsActive { get; set; }
        public bool? IsHide { get; set; }
    }
}
