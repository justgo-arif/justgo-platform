using AuthModule.Application.DTOs.Lookup;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;

namespace AuthModule.Application.Features.Lookup.Queries.GetClubTypes;

public class GetClubTypesHandler : IRequestHandler<GetClubTypesQuery, List<SelectListItemDTO<string>>>
{
    private readonly LazyService<IReadRepository<SelectListItemDTO<string>>> _repo;

    public GetClubTypesHandler(LazyService<IReadRepository<SelectListItemDTO<string>>> repo)
    {
        _repo = repo;
    }

    public async Task<List<SelectListItemDTO<string>>> Handle(GetClubTypesQuery request, CancellationToken cancellationToken)
    {
        var clubTypeSql = @"
            SELECT CD.ClubType [Value], CD.ClubType [Text]
            FROM Clubs_Default CD
            WHERE ISNULL(CD.ClubType, '') <> '' 
            GROUP BY CD.ClubType";

        var clubTypes = (await _repo.Value.GetListAsync(clubTypeSql, cancellationToken, null, null, commandType: "text")).ToList();
        return clubTypes;
    }
}
