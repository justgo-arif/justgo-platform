using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Result.Application.DTOs.ImportExportFileDtos;

namespace JustGo.Result.Application.Features.ResultUpload.Queries.GetMemberDetails;

public class GetMemberDetailsQuery : IRequest<Result<ICollection<FindMembersDto>>>
{
    public GetMemberDetailsQuery(string searchTerm)
    {
        SearchTerm = searchTerm;
    }

    public string SearchTerm { get; init; }
}