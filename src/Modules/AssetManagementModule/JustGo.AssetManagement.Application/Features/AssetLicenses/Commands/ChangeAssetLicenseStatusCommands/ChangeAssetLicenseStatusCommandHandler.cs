using Dapper;
using JustGo.AssetManagement.Application.DTOs.Common.JsonConfigDTOs;
using JustGo.AssetManagement.Application.DTOs.Common;
using JustGo.AssetManagement.Domain.Entities;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using Newtonsoft.Json;
using JustGo.AssetManagement.Application.Features.AssetRegisters.Commands.ChangeAssetStatusCommands;
using JustGo.AssetManagement.Application.Features.Common.Queries.GetLicenseStatuses;
using JustGo.AssetManagement.Application.DTOs;
using System.Data;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using LogEntityType = JustGo.Authentication.Infrastructure.Logging.EntityType;
using JustGo.Authentication.Infrastructure.Logging;
using JustGo.AssetManagement.Application.Features.AssetRegisters.Commands.AssetReinstateCommands;
using JustGo.AssetManagement.Application.Features.Common.Helpers;


namespace JustGo.AssetManagement.Application.Features.AssetRegisters.Commands.ChangeAssetLicenseStatusCommands
{
    public class ChangeAssetLicenseStatusCommandHandler : IRequestHandler<ChangeAssetLicenseStatusCommand, string>
    {

        private readonly IMediator _mediator;
        private readonly IWriteRepositoryFactory _writeRepository;
        private readonly IReadRepositoryFactory _readRepository;
        private readonly IUtilityService _utilityService;

        public ChangeAssetLicenseStatusCommandHandler(
            IMediator mediator,
            IWriteRepositoryFactory writeRepository,
            IReadRepositoryFactory readRepository,
            IUtilityService utilityService)
        {
            _mediator = mediator;
            _writeRepository = writeRepository;
            _readRepository = readRepository;
            _utilityService = utilityService;
        }

        public async Task<string> Handle(ChangeAssetLicenseStatusCommand command, CancellationToken cancellationToken)
        {
            int currentUserId = await _utilityService.GetCurrentUserId(cancellationToken);
            return await ChangeStatus(command, currentUserId, cancellationToken);
        }



        private async Task<bool> HierarkeyCheck(int clubDocId, int currentUserId, CancellationToken cancellationToken)
        {
            var adminRoles = (await _readRepository.GetLazyRepository<SelectListItemDTO<int>>().Value.GetListAsync($@" 
                                 declare @LookUpId varchar(max) = convert(varchar(max),(select LookupId from LookUp Where Name = 'Club Role'));                                       
                                 declare @RoleField varchar(20) = (select 'Field_'+convert(varchar(10),LookUpFieldid) from LookUpFields where LookupId = @LookUpId and Name = 'Name');
                                 declare @AdminField varchar(20) = (select 'Field_'+convert(varchar(10),LookUpFieldid) from LookUpFields where LookupId = @LookUpId and Name = 'IsAdmin');
                                 Execute('Select cl.'+ @RoleField +' [Text], cl.RowId [Value]  from lookup_'+@LookUpId+' cl where cl.'+ @AdminField+' = ''Yes''');
                                 ", cancellationToken, null, null, "text")).ToList();

            var dataSql = $@"WITH
                        UserHierarchyLinks as (
                                select h.[HierarchyId] [HierarchyId] from 
                                HierarchyLinks hl
                                Inner join Hierarchies h  on  h.[Id] = hl.[HierarchyId]
							    Inner join ClubMemberRoles cmr  on  cmr.UserId = hl.UserId
							    where hl.[UserId] = {currentUserId} and cmr.RoleId in ({String.Join(",", adminRoles.Select(r => r.Value))})
                        ),
                        UserClubs as (
                            SELECT DISTINCT cmr.ClubDocId ClubId
                            FROM [User]  u 
                            INNER JOIN ClubMemberRoles cmr on cmr.UserId = u.UserId
                            WHERE 
                            u.UserId = {currentUserId} and
                            cmr.RoleId in 
                            ({String.Join(",", adminRoles.Select(r => r.Value))})

                            union

                            select h.EntityId ClubId from 
                            UserHierarchyLinks hl
                            Inner join Hierarchies h  on  h.[HierarchyId].IsDescendantOf(hl.[HierarchyId]) = 1
                        )
                        Select Count(*) TotalRowCount from UserClubs uc where  uc.ClubId = {clubDocId}";

            var count = (int)await _readRepository.GetLazyRepository<string>().Value.GetSingleAsync(dataSql, cancellationToken, null, null, "text");

            return count > 0;
        }

        private async Task<string> ChangeStatus(ChangeAssetLicenseStatusCommand command, int currentUserId, CancellationToken cancellationToken)
        {
            int statusId = await _mediator.Send(new GetLicenseStatusIdQuery() { Status = command.Status });

            var queryParameters = new DynamicParameters();
            queryParameters.Add("@RecordGuid", command.AssetLicenseId);


            var assetType = await _readRepository.GetLazyRepository<AssetType>().Value
           .GetAsync($@"Select * from AssetTypes Where AssetTypeId = (
                            select AssetTypeId from AssetRegisters where AssetId = (
                                Select AssetId from AssetLicenses where RecordGuid = @RecordGuid
                            )
                        )", cancellationToken, queryParameters, null, "text");

            var typeConfig = JsonConvert.DeserializeObject<AssetTypeConfig>(assetType.AssetTypeConfig);

            var userGroups = await _readRepository.GetLazyRepository<MapItemDTO<string, string>>().Value
                        .GetListAsync($@"select 
                          g.[Name] [Key],
                          g.[Name] Value
                        from groupmembers gm 
                        inner join [Group] g on 
                        gm.GroupId= g.GroupId where 
                        gm.UserId = {currentUserId}", cancellationToken, null, null, "text");

            if (!userGroups.Any(r => typeConfig.Permission.Update.Contains(r.Value)))
            {
                return null;
            }

            var assetLicense = await _readRepository.GetLazyRepository<AssetLicense>().Value
                    .GetAsync($@"select * from AssetLicenses Where RecordGuid = @RecordGuid", cancellationToken, queryParameters, null, "text");

            var OwnerData = await _readRepository.GetLazyRepository<SelectListItemDTO<int>>().Value
                    .GetAsync($@"select 'Product' [Text], Isnull(ownerId, 0) [Value] from Products_Default where DocId = {assetLicense.ProductId}", cancellationToken, queryParameters, null, "text");



            if (!(await HierarkeyCheck(OwnerData.Value, currentUserId, cancellationToken)))
            {
                return null;
            }




            assetLicense.SetUpdateInfo(currentUserId);
            assetLicense.StatusId = statusId;

            var (sql, qparams) = SQLHelper
                .GenerateUpdateSQLWithParameters(assetLicense, "RecordGuid",
                new string[] { "AssetLicenseId", "AssetId", "CreatedBy", "CreatedDate", "RecordStatus" },
                "AssetLicenses");

            await _writeRepository.GetLazyRepository<object>().Value.ExecuteAsync(sql, qparams, null, "text");

            DynamicParameters dynamicParameters = new DynamicParameters();
            dynamicParameters.Add("@currentUserId", currentUserId);
            dynamicParameters.Add("@LicenseAssetId", assetLicense.AssetId);
            dynamicParameters.Add("@LicenseId", assetLicense.AssetLicenseId);
            await _writeRepository.GetLazyRepository<object>().Value
           .ExecuteAsync($@"exec ResolveReasonsForAssetId @UserId = @currentUserId, @AssetId = @LicenseAssetId, @LeaseId = null, @AssetLicenseId = @LicenseId",
            cancellationToken, dynamicParameters, null, "text");


            CustomLog.Event(AuditScheme.AssetManagement.Value,
                AuditScheme.AssetManagement.AssetLicense.Value,
                AuditScheme.AssetManagement.AssetLicense.StatusChanged.Value,
               currentUserId,
               assetLicense.AssetLicenseId,
               LogEntityType.Asset,
               assetLicense.AssetId,
               AuditScheme.AssetManagement.AssetLicense.StatusChanged.Name,
               "Asset License Status Changed;" + JsonConvert.SerializeObject(command)
              );


            queryParameters = new DynamicParameters();
            queryParameters.Add("@AssetId", assetLicense.AssetId);

            var asset = await _readRepository.GetLazyRepository<AssetRegister>().Value
           .GetAsync($@"Select * from AssetRegisters where AssetId = @AssetId", cancellationToken, queryParameters, null, "text");

            if (AssetStatusHelper.checkIsActionStatusId(asset.StatusId))
            {
                await _mediator.Send(new AssetStateAllocationCommand()
                {
                    AssetRegisterId = asset.RecordGuid
                });

            }


            return command.AssetLicenseId;
            

            
        }

    }
}
