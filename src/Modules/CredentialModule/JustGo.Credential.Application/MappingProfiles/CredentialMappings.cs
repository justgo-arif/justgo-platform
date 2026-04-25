using System.Reflection;
using AuthModule.Application.DTOs.Notes;
using AuthModule.Application.Features.Notes.Commands.CreateNotes;
using AuthModule.Application.Features.Notes.Commands.EditNotes;
using JustGoAPI.Shared.CustomAutoMapper;
using Mapster;

namespace JustGo.Credential.Application.MappingProfiles
{
    public class CredentialMappings : CustomMapsterProfile
    {
        public override void Register(TypeAdapterConfig config)
        {
            CreateAutoMaps(config,
                Assembly.Load("JustGo.Credential.Domain"),
                Assembly.Load("JustGo.Credential.Application"));

          
        }
    }
}