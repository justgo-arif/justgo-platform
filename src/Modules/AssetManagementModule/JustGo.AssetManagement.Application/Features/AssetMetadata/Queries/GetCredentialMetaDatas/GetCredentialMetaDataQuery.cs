using JustGo.AssetManagement.Application.DTOs;
using JustGo.AssetManagement.Domain.Entities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace JustGo.AssetManagement.Application.Features.AssetMetadata.Queries.GetCredentialMetaDatas
{
    public class GetCredentialMetaDataQuery : IRequest<List<CredentialItemMetadata>>
    { 
        public string  AssetTypeId { get; set; }
    }
}
