using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.MemberProfile.Application.Features.MemberNotes.Commands.SaveMemberNotes
{
   
    public class SaveMemberNotesCommand() : IRequest<Result<string>>
    {
        public int MemberNoteId { get; set; }
        public required string EntityId { get; set; }
        public int CategoryId { get; set; }
        public string? NoteTitle { get; set; } = string.Empty;
        public string? Details { get; set; } = string.Empty;
        public required string OwnerGuid { get; set; }
    }
}

