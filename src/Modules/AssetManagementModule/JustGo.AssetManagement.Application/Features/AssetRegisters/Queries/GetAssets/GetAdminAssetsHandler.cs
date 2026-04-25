using MapsterMapper;
using Dapper;
using JustGo.AssetManagement.Application.DTOs;
using JustGo.AssetManagement.Application.DTOs.Common;
using JustGo.AssetManagement.Application.Features.Common.Queries.GetLeaseStatuses;
using JustGo.AssetManagement.Application.Features.Common.Queries.GetLicenseStatuses;
using JustGo.AssetManagement.Application.Features.FilterHelper;
using JustGo.AssetManagement.Domain.Entities.Enums;
using JustGo.Authentication.Helper.Paginations.Offset;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using Newtonsoft.Json;
using System.Data;
using System.Text;
using JustGo.AssetManagement.Domain.Entities;
using JustGo.AssetManagement.Application.DTOs.Common.JsonConfigDTOs;
using JustGo.AssetManagement.Application.Features.AssetTypes.Queries.GetAssetMetadata;




namespace JustGo.AssetManagement.Application.Features.AssetRegisters.Queries.GetAssets
{

    public class GetAdminAssetsHandler : IRequestHandler<GetAdminAssetsQuery, PagedResult<AssetListItemDTO>>
    {
        private readonly IMediator _mediator;
        private readonly IReadRepositoryFactory _readRepository;
        private readonly IUtilityService _utilityService;
        public GetAdminAssetsHandler(
            IMediator mediator,
            IReadRepositoryFactory readRepository,
            IUtilityService utilityService) 
        {
            _mediator = mediator;
            _readRepository = readRepository;
            _utilityService = utilityService;
        }

        public async Task<PagedResult<AssetListItemDTO>> Handle(GetAdminAssetsQuery request, CancellationToken cancellationToken)
        {

            return await FetchData(request, cancellationToken);

        }

        private async Task<PagedResult<AssetListItemDTO>> FetchData(GetAdminAssetsQuery request, CancellationToken cancellationToken)
        {


            var typeId = request.SearchItems.FirstOrDefault(r => r.ColumnName == "AssetTypeId").Value;

            var type = await _mediator.Send(new AssetMetadataQuery(Guid.Parse(typeId)));

            int currentUserId = await _utilityService.GetCurrentUserId(cancellationToken);

            var dynamicParams = new DynamicParameters();
            dynamicParams.Add("currentUserId", currentUserId);
            dynamicParams.Add("RolesAllowedToViewAllAsset", type.AssetTypeConfig.RolesAllowedToViewAllAsset);

            var allowedRoles = (await _readRepository.GetLazyRepository<SelectListItemDTO<int>>()
                  .Value.GetListAsync($@"select top 1 r.[Name] [Text], ur.UserId [Value] 
                                                    from AbacUserRoles ur
                                                    inner join AbacRoles r on r.Id = ur.RoleId
                                                    where 
                                                    ur.UserId = @currentUserId and 
                                                    ( 
                                                      r.[Name] in ('System Admin', 'Asset Super Admin') or
                                                      r.[Name] in @RolesAllowedToViewAllAsset
                                                    )", cancellationToken, dynamicParams, null, "text")).ToList();



            return await _mediator.Send(new GetAssetsQuery()
            {
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                SearchItems = request.SearchItems,
                SortItems = request.SortItems,
                SkipHierarchyAssetsMode = allowedRoles.Any()
            });


        }

    }
}
