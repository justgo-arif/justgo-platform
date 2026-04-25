using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Organisation.Application.DTOs;

namespace JustGo.Organisation.Application.Features.Organizations.Queries.GetClubDetails;

public class GetClubDetailsQuery : IRequest<ClubDetailsDto?>
{
    public Guid ClubGuid { get; set; }
    public Guid UserGuid { get; set; }

    public GetClubDetailsQuery(Guid clubGuid, Guid userGuid)
    {
        ClubGuid = clubGuid;
        UserGuid = userGuid;
    }
}

