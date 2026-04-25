using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Credential.Application.DTOs;

namespace JustGo.Credential.Application.Features.Credentials.Queries.GetCredentialSummary;

public class GetCredentialSummaryQuery : IRequest<CredentialSummaryDto>
{
    public GetCredentialSummaryQuery(Guid userGuid)
    {
        UserGuid = userGuid;
    }
    public Guid UserGuid { get; set; }
}
