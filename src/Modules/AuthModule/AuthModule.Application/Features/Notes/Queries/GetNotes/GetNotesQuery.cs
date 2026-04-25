using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AuthModule.Application.DTOs.Notes;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace AuthModule.Application.Features.Notes.Queries.GetNotes
{
    public class GetNotesQuery : IRequest<List<NoteDTO>>
    {
        public GetNotesQuery(int entityType, Guid entityId, string module)
        {
            EntityType = entityType;
            EntityId = entityId;
            Module = module;
        }

        public int EntityType { get; set; }
        public Guid EntityId { get; set; }
        public string Module { get; set; }
    }
}
