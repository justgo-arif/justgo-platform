using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Result.Application.DTOs.ImportExportFileDtos;

namespace JustGo.Result.Application.Features.MemberUpload.Queries.GetMemberDetails
{
    public record GetMemberDetailsByIdQuery : IRequest<MemberDetailsDto?>
    {
        public int Id { get; init; }
    }

}
