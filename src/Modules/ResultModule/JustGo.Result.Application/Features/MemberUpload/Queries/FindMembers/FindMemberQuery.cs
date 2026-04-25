using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Result.Application.DTOs.ImportExportFileDtos;

namespace JustGo.Result.Application.Features.MemberUpload.Queries.FindMembers
{
    public class FindMemberQuery : IRequest<List<FindMembersDto>>
    {
        public string SearchTerm { get; set; } = string.Empty;
        public FindMemberQuery()
        {
        }
        public FindMemberQuery(string? searchTerm)
        {
            SearchTerm = searchTerm ?? string.Empty;
        }
    }
}
