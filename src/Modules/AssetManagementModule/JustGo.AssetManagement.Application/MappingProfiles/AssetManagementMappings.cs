using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using JustGo.AssetManagement.Application.DTOs;
using JustGo.AssetManagement.Application.Features.AssetLeases.Commands.CreateLeases;
using JustGo.AssetManagement.Application.Features.AssetRegisters.Commands.EditAssets;
using JustGo.AssetManagement.Application.Features.AssetRegisters.Commands.RegisterAssets;
using JustGo.AssetManagement.Domain.Entities;
using JustGoAPI.Shared.CustomAutoMapper;
using Mapster;

namespace JustGo.AssetManagement.Application.MappingProfiles
{
    public class AssetManagementMappings: CustomMapsterProfile
    {       
        public override void Register(TypeAdapterConfig config)
        {
            CreateAutoMaps(config,
                Assembly.Load("JustGo.AssetManagement.Domain"),
                Assembly.Load("JustGo.AssetManagement.Application"));

            config.NewConfig<CreateAssetLeaseCommand, AssetLease>().TwoWays();
            config.NewConfig<LeaseDTOWithRawData, LeaseListItemDTO>().TwoWays();
            config.NewConfig<AssetDTOWithRawData, AssetListItemDTO>().TwoWays();
            config.NewConfig<AssetDTOWithRawData, AssetDTO>().TwoWays();
            config.NewConfig<ActionRequiredRawItemDTO, ActionRequiredItemDTO>().TwoWays();
            config.NewConfig<AssetRegisterCommand, AssetRegister>().TwoWays();
            config.NewConfig<EditAssetCommand, AssetRegister>().TwoWays();
            config.NewConfig<LeaseActivityLogRawItemDTO, LeaseActivityLogItemDTO>().TwoWays();
        }
    }
}
