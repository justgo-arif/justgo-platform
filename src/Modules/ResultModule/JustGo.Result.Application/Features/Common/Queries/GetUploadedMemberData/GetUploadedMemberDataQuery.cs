using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Result.Domain.Entities;

namespace JustGo.Result.Application.Features.Common.Queries.GetUploadedMemberData
{
    public class GetUploadedMemberDataQuery : IRequest<ResultUploadedMemberData?>
    {
        public int UploadedMemberDataId { get; set; }
    }
}
