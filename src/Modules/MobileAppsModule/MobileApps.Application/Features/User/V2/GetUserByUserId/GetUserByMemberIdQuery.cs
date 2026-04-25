using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AuthModule.Domain.Entities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using MobileApps.Domain.Entities;

namespace MobileApps.Application.Features.Members.Queries.GetMemberByMemberDocId
{
    public class GetUserByMemberIdQuery : IRequest<UserViewModel>
    {
        public int MemberDocId { get; set; }
        public GetUserByMemberIdQuery(int id)
        {
            MemberDocId = id;
        }
      
    }
}
