using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.MemberProfile.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.MemberProfile.Application.Features.MemberNotes.Queries.GetMemberNotes
{
    public class GetMemberNotesQuery : IRequest<GetMemberNotesDto>
    {
        public required string OwnerGuid { get; set; }
        public required string UserSyncId { get; set; }
        public int CategoryId { get; set; }
    }
}
