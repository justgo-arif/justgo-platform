using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.MemberProfile.Application.DTOs;
using Microsoft.AspNetCore.Http;

namespace JustGo.MemberProfile.Application.Features.Members.Commands.SetMemberPhoto;

public class SetMemberPhotoCommand : IRequest<OperationResultDto<string>>
{
    public required IFormFile File { get; set; }
    public required Guid UserSyncId { get; set; }
}
