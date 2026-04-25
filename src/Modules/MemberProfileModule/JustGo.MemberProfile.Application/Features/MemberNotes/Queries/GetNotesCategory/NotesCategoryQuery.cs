using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.FieldManagement.Domain.Entities;
using JustGo.MemberProfile.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.MemberProfile.Application.Features.MemberNotes.Queries.GetNotesCategory
{
    public class NotesCategoryQuery : IRequest<List<NoteCategories>>
    {
    }
}
