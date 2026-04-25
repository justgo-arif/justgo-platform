using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Organisation.Application.DTOs;

namespace JustGo.Organisation.Application.Features.Organizations.Queries.GetClubDetails;

public class GetMemberPrimaryClubDetailsQuery : IRequest<IEnumerable<PrimaryClubDto>>
{
    public Guid UserGuid { get; set; }
    
}

