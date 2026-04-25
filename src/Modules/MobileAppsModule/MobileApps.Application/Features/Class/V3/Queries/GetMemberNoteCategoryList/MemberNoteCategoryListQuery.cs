using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace MobileApps.Application.Features.Class.V3.Queries.GetMemberNoteCategoryList
{
    public class MemberNoteCategoryListQuery : IRequest<IList<IDictionary<string,object>>>
    {
        public int MemberNoteId { get; set; }
    }
}
