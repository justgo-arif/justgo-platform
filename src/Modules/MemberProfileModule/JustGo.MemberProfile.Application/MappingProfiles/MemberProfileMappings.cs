using JustGo.MemberProfile.Application.DTOs;
using JustGo.MemberProfile.Domain.Entities;
using JustGoAPI.Shared.CustomAutoMapper;
using Mapster;
using System.Reflection;

namespace JustGo.MemberProfile.Application.MappingProfiles;

public class MemberProfileMappings : CustomMapsterProfile
{
    public override void Register(TypeAdapterConfig config)
    {
        CreateAutoMaps(config,
            Assembly.Load("JustGo.MemberProfile.Domain"),
            Assembly.Load("JustGo.MemberProfile.Application"));

        config.NewConfig<MemberSummary, MemberSummaryDto>().TwoWays();
    }
}
