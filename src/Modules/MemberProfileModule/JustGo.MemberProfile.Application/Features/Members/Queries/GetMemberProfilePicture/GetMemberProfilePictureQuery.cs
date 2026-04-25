using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.MemberProfile.Application.Features.Members.Queries.GetMemberProfilePicture
{
    public class GetMemberProfilePictureQuery : IRequest<string>
    {
        public GetMemberProfilePictureQuery(Guid id)
        {
            Id = id;
        }

        public Guid Id { get; set; }

    }
}
