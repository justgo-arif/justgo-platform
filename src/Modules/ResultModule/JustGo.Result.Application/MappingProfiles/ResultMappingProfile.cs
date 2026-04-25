using System.Reflection;
using JustGoAPI.Shared.CustomAutoMapper;
using Mapster;

namespace JustGo.Result.Application.MappingProfiles
{
    public class ResultMappingProfile : CustomMapsterProfile
    {
        public override void Register(TypeAdapterConfig config)
        {
            CreateAutoMaps(config,
                Assembly.Load("JustGo.Result.Domain"),
                Assembly.Load("JustGo.Result.Application"));

            
        }
    }
}
