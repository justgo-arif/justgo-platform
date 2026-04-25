using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Credential.Application.DTOs;
using JustGo.MemberProfile.Application.DTOs;

namespace JustGo.Credential.Application.Features.Credentials.Queries.GetCredentialsMetaData;

public class GetCredentialsMetaDataQuery : IRequest<OperationResultDto<FilterMetaDataDTO>>
{
    public GetCredentialsMetaDataQuery(Guid id)
    {
        Id = id;
    }
    public Guid Id { get; }
}


