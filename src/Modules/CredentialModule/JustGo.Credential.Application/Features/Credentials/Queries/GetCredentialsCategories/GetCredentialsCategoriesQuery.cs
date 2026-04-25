using JustGo.Credential.Application.DTOs;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.Credential.Application.Features.Credentials.Queries.GetCredentialsCategories
{
    public class GetCredentialsCategoriesQuery : IRequest<List<CredentialsCategoriesDto>>
    {
        public GetCredentialsCategoriesQuery()
        {
        }
    }   
}


