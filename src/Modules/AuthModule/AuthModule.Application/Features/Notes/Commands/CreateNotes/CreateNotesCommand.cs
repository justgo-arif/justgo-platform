using JustGo.Authentication.Services.Interfaces.CustomMediator;
using System.ComponentModel;

namespace AuthModule.Application.Features.Notes.Commands.CreateNotes
{
    public class CreateNotesCommand : IRequest<int>
    {
        public int EntityType { get; set; }
        public Guid EntityId { get; set; }
        public string Details { get; set; }
        public string Module { get; set; }
        [DefaultValue(false)]
        public bool? IsMailSend { get; set; }
    }
}
