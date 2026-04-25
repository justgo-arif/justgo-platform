using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.FileSystemManager.AzureBlob;
using JustGo.Authentication.Services.Interfaces.Infrastructure.SystemSettings;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.MemberProfile.Domain.Entities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.MemberProfile.Application.Features.Members.Queries.GetMemberProfilePicture
{
    public class GetMemberProfilePictureHandler : IRequestHandler<GetMemberProfilePictureQuery, string>
    {
        private readonly IProfilePictureService _profilePictureService;
        public GetMemberProfilePictureHandler(IProfilePictureService profilePictureService)
        {
            _profilePictureService = profilePictureService;
        }
        public async Task<string> Handle(GetMemberProfilePictureQuery request, CancellationToken cancellationToken)
        {
            var imageUrl = await _profilePictureService.GetProfilePictureUrlAsync(request.Id, cancellationToken);
            return imageUrl;           
        }
    }
}
